import os
import shutil
from typing import Tuple

from setuptools import setup
from setuptools.command.bdist_wheel import bdist_wheel
from setuptools.command.build_ext import build_ext
from setuptools.dist import Distribution


def copy_binary_files(net_rid: str):
    local_dir = os.path.dirname(os.path.realpath(__file__))
    project_dir = os.path.dirname(os.path.dirname(local_dir))
    publish_dir = os.path.join(
        project_dir,
        "TypeTreeGeneratorAPI",
        "bin",
        "Release",
        "net9.0",
        net_rid,
        "publish",
    )
    assert os.path.isdir(publish_dir), f"Couldn't find publish dir for {net_rid}"
    target_dir = os.path.join(local_dir, "TypeTreeGeneratorAPI")

    for item in os.listdir(publish_dir):
        if item.endswith((".dll", ".so", ".dylib")):
            shutil.copy(os.path.join(publish_dir, item), os.path.join(target_dir, item))


def copy_binary_files_plat(platform_tag: str):
    if platform_tag.startswith("win"):
        net_rid = BDIST_TAG_MAP_WIN[platform_tag]
    elif platform_tag.startswith("macosx"):
        net_rid = next((v for k, v in BDIST_TAG_MAP_MAC.items() if platform_tag.endswith(k)))
    else:
        net_rid = next((v for k, v in BDIST_TAG_MAP_LINUX.items() if platform_tag.endswith(k)))
        if "musllinux" in platform_tag:
            net_rid = net_rid.replace("-", "-musl-")
    copy_binary_files(net_rid)


# see:
# https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
BDIST_TAG_MAP_WIN = {
    "win32": "win-x86",
    "win_amd64": "win-x64",
    "win_arm64": "win-arm64",
}

BDIST_TAG_MAP_MAC = {"arm64": "osx-arm64", "x86_64": "osx-x64"}

BDIST_TAG_MAP_LINUX = {
    "x86_64": "linux-x64",
    "aarch64": "linux-arm64",
    "armv7l": "linux-arm",
}


class bdist_wheel_abi3(bdist_wheel):
    def run(self):
        platform_tag = self.get_tag()[2]
        copy_binary_files_plat(platform_tag)
        super().run()

    def get_tag(self) -> Tuple[str, str, str]:
        self.root_is_pure = False
        python, abi, plat = super().get_tag()
        self.root_is_pure = True
        if python.startswith("cp"):
            # on CPython, our wheels are abi3 and compatible back to 3.7
            return "cp36", "abi3", plat
        return python, abi, plat


class build_ext_c(build_ext):
    def run(self):
        platform_tag = self.plat_name.replace("-", "_")
        copy_binary_files_plat(platform_tag)
        super().run()


class UnpureDistribution(Distribution):
    """Distribution which is not marked as pure, so that wheels are built as platform-specific"""

    def is_pure(self) -> bool:
        return False


setup(
    cmdclass={"bdist_wheel": bdist_wheel_abi3, "build_ext": build_ext_c},
    distclass=UnpureDistribution,
    zip_safe=False,
)
