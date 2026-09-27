#!/usr/bin/env python3
"""Native Wayland scaling/modal regression sweep in an isolated headless Sway session.

Inspired by PR #13189's conformance scripts; uses this port's real dialog and backend setup.
No display settings, user repositories or application settings are changed.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import time


def wait_for(get_value, description, timeout=15):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        value = get_value()
        if value:
            return value
        time.sleep(0.1)
    raise RuntimeError(f"Timed out waiting for {description}")


def stop(process):
    if process is not None and process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path, help="Directory for reports, logs and optional screenshots")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    (output / "summary.json").unlink(missing_ok=True)
    for tool in ("dotnet", "sway", "swaymsg", "setpriv"):
        if not shutil.which(tool):
            raise SystemExit(f"Required tool is missing: {tool}")
    subprocess.run(["dotnet", "build", str(Path(__file__).with_name("WaylandSmoke.csproj")),
                    "-p:UseAppHost=false", "--nologo", "-v:q"], cwd=repo, check=True)
    app = repo / "artifacts/Debug/bin/WaylandSmoke/net10.0/WaylandSmoke.dll"
    compositor = probe = None
    with tempfile.TemporaryDirectory(prefix="ge-wayland-qa-") as temporary:
        scratch = Path(temporary)
        runtime = scratch / "runtime"
        runtime.mkdir(mode=0o700)
        config = scratch / "sway.conf"
        config.write_text('''xwayland disable
swaybg_command -
seat seat0 fallback true
output HEADLESS-1 mode 1600x1000 position 0 0 scale 1.25
output HEADLESS-2 mode 1800x1200 position 1280 0 scale 1.5
for_window [title="^Git Extensions Wayland QA"] floating enable
''')
        env = dict(os.environ)
        for key in ("DISPLAY", "WAYLAND_DISPLAY", "SWAYSOCK", "AVALONIA_SCREEN_SCALE_FACTORS", "GE_AVALONIA_THEME"):
            env.pop(key, None)
        env.update(XDG_RUNTIME_DIR=str(runtime), XDG_CONFIG_HOME=str(scratch / "config"),
                   XDG_CACHE_HOME=str(scratch / "cache"), XDG_DATA_HOME=str(scratch / "data"),
                   WLR_BACKENDS="headless", WLR_HEADLESS_OUTPUTS="2", WLR_RENDERER="pixman",
                   WLR_LIBINPUT_NO_DEVICES="1", GITEXTENSIONS_USE_WAYLAND="1")
        with (output / "sway.log").open("w") as compositor_log:
            try:
                # Arch installs Sway with cap_sys_nice. With RLIMIT_RTTIME=0 it is
                # immediately killed after enabling realtime scheduling. This isolated QA
                # compositor needs no elevated capabilities, regardless of the host limits.
                compositor = subprocess.Popen(["setpriv", "--no-new-privs", "sway", "--unsupported-gpu", "--config", str(config)],
                                              env=env, stdout=compositor_log, stderr=subprocess.STDOUT, start_new_session=True)
                socket = wait_for(lambda: next(runtime.glob("sway-ipc.*.sock"), None), "Sway IPC")
                wayland = wait_for(lambda: next((p for p in runtime.glob("wayland-*") if p.is_socket()), None), "Wayland socket")
                env.update(SWAYSOCK=str(socket), WAYLAND_DISPLAY=wayland.name)

                def sway(*command):
                    return subprocess.check_output(["swaymsg", "-s", str(socket), *command], env=env, text=True)

                def ready_outputs():
                    if compositor.poll() is not None:
                        raise RuntimeError(f"Sway exited: {compositor.returncode}; see sway.log")
                    result = subprocess.run(["swaymsg", "-s", str(socket), "-t", "get_outputs", "-r"],
                                            env=env, text=True, capture_output=True)
                    return json.loads(result.stdout) if result.returncode == 0 else None

                outputs = wait_for(ready_outputs, "configured Sway outputs")
                assert {o["name"]: o["scale"] for o in outputs} == {"HEADLESS-1": 1.25, "HEADLESS-2": 1.5}, outputs
                (output / "outputs.json").write_text(json.dumps(outputs, indent=2))
                results = []
                for theme in ("classic", "modern"):
                    for color in ("light", "dark"):
                        prefix = output / f"{theme}-{color}"
                        report = prefix.with_suffix(".json")
                        report.unlink(missing_ok=True)
                        command = scratch / "command.txt"
                        command.write_text("")
                        sway("focus output HEADLESS-1")
                        with prefix.with_suffix(".log").open("w") as app_log:
                            probe_env = dict(env, WAYLAND_DEBUG="1")
                            probe = subprocess.Popen(["dotnet", str(app), str(report), str(command), theme, color],
                                                     env=probe_env, stdout=app_log, stderr=subprocess.STDOUT)

                            def snapshot(predicate):
                                if probe.poll() is not None:
                                    raise RuntimeError(f"Probe exited: {probe.returncode}; see {prefix}.log")
                                try:
                                    data = json.loads(report.read_text())
                                except (FileNotFoundError, json.JSONDecodeError):
                                    return None
                                return data if predicate(data) else None

                            first = wait_for(lambda: snapshot(lambda d: d["Root"]["RenderScaling"] == 1.25), "125% dialog")
                            sway('[title="^Git Extensions Wayland QA$"] move container to output HEADLESS-2')
                            second = wait_for(lambda: snapshot(lambda d: d["Root"]["RenderScaling"] == 1.5), "150% dialog")
                            command.write_text("modal")
                            modal = wait_for(lambda: snapshot(lambda d: d["Modal"] and d["ModalOwnedByRoot"] and not d["ModalTaskCompleted"]), "modal ownership")
                            sway('[title="^Git Extensions Wayland QA modal$"] move container to output HEADLESS-1')
                            moved_modal = wait_for(lambda: snapshot(lambda d: d["Modal"] and d["Modal"]["RenderScaling"] == 1.25), "125% modal")
                            if shutil.which("grim"):
                                subprocess.run(["grim", str(prefix.with_suffix(".png"))], env=env, check=True)
                            command.write_text("close-modal")
                            wait_for(lambda: snapshot(lambda d: d["Command"] == "close-modal" and d["Modal"] is None and d["ModalTaskCompleted"]), "modal completion")
                            sway('[title="^Git Extensions Wayland QA$"] move container to output HEADLESS-1')
                            returned = wait_for(lambda: snapshot(lambda d: d["Root"]["RenderScaling"] == 1.25), "125% round trip")
                            for dimension in ("Width", "Height"):
                                assert abs(first["Root"]["ClientSize"][dimension] - returned["Root"]["ClientSize"][dimension]) <= 1, (first, returned)
                            command.write_text("stop")
                            probe.wait(timeout=10)
                            assert probe.returncode == 0, probe.returncode
                            probe = None
                        protocol = prefix.with_suffix(".log").read_text()
                        assert "wl_surface" in protocol, "No native Wayland protocol observed"
                        result = dict(theme=theme, color=color, first=first, second=second,
                                      modal=modal, movedModal=moved_modal, returned=returned)
                        prefix.with_suffix(".stages.json").write_text(json.dumps(result, indent=2))
                        results.append(result)
                        print(f"PASS {theme}/{color}: 125% → 150% → 125%, modal ownership and stable dialog size", flush=True)
                (output / "summary.json").write_text(json.dumps(dict(passed=len(results), results=results), indent=2))
            finally:
                stop(probe)
                stop(compositor)


if __name__ == "__main__":
    main()
