#!/usr/bin/env bash
set -euo pipefail

# Run from any directory. On Linux, --bundle-only permits cross-publish validation;
# release archives must be signed and verified on macOS.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
rid="${1:?Usage: build.sh <osx-arm64|osx-x64> <version> [--bundle-only]}"
version="${2:?Specify a version, e.g. 0.1.0 or 0.1.0-preview.1}"
mode="${3:-}"
case "$rid" in osx-arm64|osx-x64) ;; *) echo "Unsupported runtime: $rid" >&2; exit 1 ;; esac
if [[ ! "$version" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$ ]]; then
    echo "Expected a version such as 0.1.0 or 0.1.0-preview.1" >&2
    exit 1
fi
if [[ "$mode" != "" && "$mode" != "--bundle-only" ]]; then
    echo "Unknown option: $mode" >&2
    exit 1
fi
if [[ "$mode" != "--bundle-only" && "$(uname -s)" != "Darwin" ]]; then
    echo "Release signing requires macOS. Use --bundle-only for cross-publish validation." >&2
    exit 1
fi

cd "$repo_root"
work_dir="$repo_root/artifacts/macos/$rid"
# Isolated outputs prevent an earlier platform/build from leaking into a release.
rm -rf "$work_dir"
mkdir -p "$work_dir"

# Retain the solution's plugin build dependencies, without building test hosts
# and the Windows installer (several GiB of unnecessary release-runner output).
python3 - "$work_dir/Gitanas.slnf" <<'PYTHON'
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

solution = Path('GitExtensions.slnx').resolve()
projects = [p.attrib['Path'] for p in ET.parse(solution).iter('Project')
            if p.attrib['Path'].endswith('.csproj') and not p.attrib['Path'].startswith('tests/')]
Path(sys.argv[1]).write_text(json.dumps({'solution': {'path': str(solution), 'projects': projects}}))
PYTHON
dotnet build "$work_dir/Gitanas.slnf" -c Release -p:UseAppHost=false \
    "-p:ArtifactsDir=$work_dir/build/"
dotnet publish src/app/GitExtensions/GitExtensions.csproj -c Release -r "$rid" \
    --self-contained true -p:MacOSBundle=true \
    -p:RuntimeFrameworkVersion= -p:TargetLatestRuntimePatch=true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false \
    "-p:ArtifactsDir=$work_dir/build/" -o "$work_dir/publish"

# The crash reporter also runs out of process. Publish its executable separately
# from the app's project-reference graph, then share the app's bundled runtime.
dotnet publish src/app/BugReporter/BugReporter.csproj -c Release -r "$rid" \
    --self-contained true -p:IsPublishable=true \
    -p:RuntimeFrameworkVersion= -p:TargetLatestRuntimePatch=true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false \
    "-p:ArtifactsDir=$work_dir/reporter-build/" -o "$work_dir/reporter-publish"

args=(--rid "$rid" --version "$version" --work-dir "$work_dir")
if [[ "$mode" == "--bundle-only" ]]; then args+=(--bundle-only); fi
python3 eng/macos/package.py "${args[@]}"
