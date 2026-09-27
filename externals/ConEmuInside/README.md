# ConEmuInside

The console emulator session of [conemu-inside](https://github.com/gitextensions/conemu-inside) (`ConEmuWinForms` at
[commit 769053b](https://github.com/gitextensions/conemu-inside/tree/769053bb1912d64c8ead6897bec142425b6630a7)), without WinForms: the WinForms `ConEmuControl` is
replaced by `ConEmuHost`, which embeds ConEmu in any native window, and the WinForms timer of `ConEmuSession` by a
thread pool timer that posts to the home thread (docs/avalonia-port/PLAN.md, phase 8).

The required sources are vendored here; building does not require the original
`conemu-inside` submodule.

## License (BSD 3-clause)

    Copyright (c) 2021, Maksim Moisiuk <ConEmu.Maximus5@gmail.com>

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:
    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.
    3. The name of the authors may not be used to endorse or promote products
       derived from this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
    IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
    OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
    IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
    INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
    NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
    DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
    THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
    THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
