import ctypes
import errno
import fcntl
import json
import os
from pathlib import Path
import platform
import shutil
import sys
import uuid

base = Path(sys.argv[1]) / ("api-" + uuid.uuid4().hex)
base.mkdir()
libc = ctypes.CDLL("libc.so.6", use_errno=True)
libc.openat.argtypes = [ctypes.c_int, ctypes.c_char_p, ctypes.c_int, ctypes.c_uint]
libc.openat.restype = ctypes.c_int
libc.renameat.argtypes = [ctypes.c_int, ctypes.c_char_p, ctypes.c_int, ctypes.c_char_p]
libc.renameat.restype = ctypes.c_int
flags = {n: getattr(os, n) for n in ["O_RDONLY", "O_WRONLY", "O_CREAT", "O_EXCL", "O_NOFOLLOW",
          "O_CLOEXEC", "O_DIRECTORY", "O_NONBLOCK"]}
directory = None
try:
    root, outside = base / "root", base / "outside"
    root.mkdir()
    outside.mkdir()
    directory = os.open(root, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC)
    fcntl.flock(directory, fcntl.LOCK_EX | fcntl.LOCK_NB)
    os.mkdir("registration", dir_fd=directory)
    os.symlink(outside, root / "alias")
    descriptor = libc.openat(directory, b"alias", os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC, 0)
    assert descriptor == -1 and ctypes.get_errno() in (errno.ELOOP, errno.ENOTDIR)
    descriptor = libc.openat(directory, b"new", os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW | os.O_CLOEXEC, 0o600)
    assert descriptor >= 0
    os.write(descriptor, b"owned")
    os.fsync(descriptor)
    os.close(descriptor)
    assert libc.renameat(directory, b"new", directory, b"latest") == 0
    os.rename(root, base / "held")
    os.symlink(outside, root)
    descriptor = libc.openat(directory, b"anchored", os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    assert descriptor >= 0
    os.close(descriptor)
    assert not list(outside.iterdir())
    print(json.dumps({"platform": platform.platform(), "libc": platform.libc_ver(), "flags": flags,
                      "LOCK_EX": fcntl.LOCK_EX, "LOCK_NB": fcntl.LOCK_NB,
                      "ENOENT": errno.ENOENT, "EEXIST": errno.EEXIST, "ELOOP": errno.ELOOP,
                      "ENOTDIR": errno.ENOTDIR, "root": str(base),
                      "filesystem": os.statvfs(base).f_fsid,
                      "assertions": "nofollow, exclusive create, fsync, flock, renameat, replacement anchored: passed"}, indent=2))
finally:
    if directory is not None:
        os.close(directory)
    shutil.rmtree(base)
