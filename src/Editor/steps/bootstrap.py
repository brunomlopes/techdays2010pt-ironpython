class ProgrammingLanguage: pass
class Python(ProgrammingLanguage): pass
class CLR: pass
class IronPython(Python, CLR):
    version = 2.6
    