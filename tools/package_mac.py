"""Package a Windows cross-built .app with Unix executable permissions."""
import argparse
import hashlib
import plistlib
import stat
import struct
import zipfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("app", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
app = args.app.resolve()
info = plistlib.loads((app / "Contents/Info.plist").read_bytes())
assert info["CFBundleShortVersionString"] == "0.1.0"
launcher = app / "Contents/MacOS" / info["CFBundleExecutable"]
assert launcher.is_file()
native = []
for path in app.rglob("*"):
    if not path.is_file():
        continue
    with path.open("rb") as stream:
        head = stream.read(8)
    if head[:4] == b"\xcf\xfa\xed\xfe":
        assert struct.unpack("<I", head[4:8])[0] == 0x0100000C, f"Not ARM64: {path}"
        native.append(path)
    elif head[:4] in (b"\xca\xfe\xba\xbe", b"\xbe\xba\xfe\xca", b"\xfe\xed\xfa\xcf"):
        raise RuntimeError(f"Unexpected native architecture: {path}")
assert launcher in native
assert any(p.name == "TuanjiePlayer.dylib" for p in native)
assert not args.output.exists(), "Refusing to overwrite an existing package"
readme = """KaceLab — v0.1.0_CE_Beta
macOS Apple Silicon (ARM64), Metal renderer, teacher edition.

在 Apple Silicon Mac 上解压后打开 KaceLab.app。
应用内版本：v0.1.0_CE_Beta；系统 Bundle 版本：0.1.0。
本包在 Windows 上交叉构建并核验 ARM64 架构与 ZIP 完整性，尚未在 Mac 真机运行验收。
没有进行 Apple Developer ID 发布签名或公证，macOS 可能提示来源验证警告。
不内置 AI API 密钥；AI 功能需要使用者自行配置。
ZIP 保留了 macOS 可执行权限，请在 Mac 上解压。
"""
with zipfile.ZipFile(args.output, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted(app.rglob("*")):
        name = path.relative_to(app.parent).as_posix()
        entry = zipfile.ZipInfo(name + ("/" if path.is_dir() else ""))
        entry.create_system = 3
        entry.compress_type = zipfile.ZIP_DEFLATED
        mode = (stat.S_IFDIR | 0o755) if path.is_dir() else (stat.S_IFREG | (0o755 if path in native else 0o644))
        entry.external_attr = mode << 16
        if path.is_dir():
            entry.external_attr |= 0x10
        archive.writestr(entry, b"" if path.is_dir() else path.read_bytes())
    archive.writestr("README.txt", readme.encode("utf-8"))
with zipfile.ZipFile(args.output) as archive:
    assert archive.testzip() is None
    executable = archive.getinfo(launcher.relative_to(app.parent).as_posix())
    assert (executable.external_attr >> 16) & 0o111
print(f"Verified {len(native)} ARM64 Mach-O files and ZIP CRCs")
print(f"Package: {args.output}")
print(f"Bytes: {args.output.stat().st_size}")
print(f"SHA256: {hashlib.file_digest(args.output.open('rb'), 'sha256').hexdigest()}")
