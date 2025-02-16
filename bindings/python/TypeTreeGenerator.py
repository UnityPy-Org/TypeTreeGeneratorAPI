import ctypes


class TypeTreeNode(ctypes.Structure):
    _pack_ = 4
    _fields_ = [
        ("m_Type", ctypes.c_char_p),
        ("m_Name", ctypes.c_char_p),
        ("m_Level", ctypes.c_int),
        ("m_MetaFlag", ctypes.c_int),
    ]


DLL: ctypes.CDLL


def init_dll(fp: str):
    global DLL
    dll = ctypes.WinDLL(fp)
    # set function types
    dll.TypeTreeGenerator_init.argtypes = [ctypes.c_char_p]
    dll.TypeTreeGenerator_init.restype = ctypes.c_void_p
    dll.TypeTreeGenerator_loadDLL.argtypes = [
        ctypes.c_void_p,
        ctypes.c_char_p,
        ctypes.c_int,
    ]
    dll.TypeTreeGenerator_loadIL2CPP.argtypes = [
        ctypes.c_void_p,
        ctypes.c_char_p,
        ctypes.c_int,
        ctypes.c_char_p,
        ctypes.c_int,
    ]
    dll.TypeTreeGenerator_generateTreeNodesJson.argtypes = [
        ctypes.c_void_p,
        ctypes.c_char_p,
        ctypes.c_char_p,
        ctypes.c_void_p,
        ctypes.c_void_p,
    ]
    dll.TypeTreeGenerator_generateTreeNodesRaw.argtypes = [
        ctypes.c_void_p,
        ctypes.c_char_p,
        ctypes.c_char_p,
        ctypes.POINTER(ctypes.POINTER(TypeTreeNode)),
        ctypes.POINTER(ctypes.c_int),
    ]
    dll.TypeTreeGenerator_del.argtypes = [ctypes.c_void_p]
    dll.FreeCoTaskMem.argtypes = [ctypes.c_void_p]
    DLL = dll  # type: ignore


class TypeTreeGenerator:
    ptr = ctypes.c_void_p

    @staticmethod
    def load_generator_dll(fp: str):
        init_dll(fp)

    def __init__(self, unity_version: str):
        assert (
            DLL is not None
        ), "TypeTreeGenerator DLL wasn't loaded yet via the static method TypeTreeGenerator.load_generator_dll(fp)!"
        self.ptr = DLL.TypeTreeGenerator_init(unity_version.encode("ascii"))

    def __del__(self):
        DLL.TypeTreeGenerator_del(self.ptr)

    def load_dll(self, dll: bytes):
        assert not DLL.TypeTreeGenerator_loadDLL(
            self.ptr, dll, len(dll)
        ), "failed to load dll"

    def load_il2cpp(self, il2cpp: bytes, metadata: bytes):
        assert not DLL.TypeTreeGenerator_loadIL2CPP(
            self.ptr, il2cpp, metadata
        ), "failed to load il2cpp"

    def get_nodes_as_json(self, assembly: str, fullname: str) -> str:
        jsonPtr = ctypes.c_char_p()
        jsonLen = ctypes.c_int()
        assert not DLL.TypeTreeGenerator_generateTreeNodesJson(
            self.ptr,
            assembly.encode("ascii"),
            fullname.encode("ascii"),
            ctypes.byref(jsonPtr),
            ctypes.byref(jsonLen),
        ), "failed to dump nodes as json"
        data = jsonPtr.value
        assert (
            data and len(data) != jsonLen.value
        ), "returned json data has an unexpected length"
        json_str = data.decode("utf8")
        DLL.FreeCoTaskMem(jsonPtr)
        return json_str

    def get_nodes_as_dict(self, assembly: str, fullname: str) -> dict:
        nodes_ptr = ctypes.POINTER(TypeTreeNode)()
        nodes_count = ctypes.c_int()
        assert not DLL.TypeTreeGenerator_generateTreeNodesJson(
            assembly.encode("ascii"),
            fullname.encode("ascii"),
            ctypes.byref(nodes_ptr),
            ctypes.byref(nodes_count),
        ), "failed to dump nodes raw"
        nodes_array = ctypes.cast(
            nodes_ptr, ctypes.POINTER(TypeTreeNode * nodes_count.value)
        ).contents
        nodes = [
            {
                "m_Type": node.m_Type.decode("ascii"),
                "m_Name": node.m_Name.decode("ascii"),
                "m_Level": node.m_Level,
                "m_MetaFlag": node.m_MetaFlag,
            }
            for node in nodes_array
        ]
        DLL.FreeCoTaskMem(nodes_ptr)
        return nodes


__all__ = [TypeTreeGenerator]  # type: ignore
