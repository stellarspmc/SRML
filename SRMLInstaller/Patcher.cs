using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace SRMLInstaller;

internal class Patcher : IDisposable {
    private AssemblyDefinition _curAssembly;
    private MethodDefinition _target;
    private readonly MethodReference _methodToPatchIn;
    private readonly string _filename;
    public Patcher(string filename,MethodReference methodToPatchIn) {
        Init(filename);
        _methodToPatchIn = methodToPatchIn;
        _filename = filename;
    }

    private void Init(string filename) {
        try {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(filename));
            _curAssembly = AssemblyDefinition.ReadAssembly(filename,new ReaderParameters { AssemblyResolver = resolver });
        } catch {
            throw new Exception($"Couldn't find {filename}!");
        }

        _target = FindTarget();
    }

    private MethodDefinition FindTarget() => _curAssembly.MainModule.GetType("GameContext").Methods.First(x => x.Name == "Awake");
    public bool IsPatched() => _target.Body.Instructions[0].OpCode == OpCodes.Call && _target.Body.Instructions[0].Operand is MethodReference methRef && (methRef.Name=="LoadSRModLoader" || methRef.Name=="PreLoad");
    private void RemoveOldPatch() {
        if(_target.Body.Instructions[0].Operand is MethodReference { Name: "LoadSRML" }) _target.Body.GetILProcessor().Remove(_target.Body.Instructions[0]);
        if (_target.DeclaringType.Methods.FirstOrDefault(x => x.Name == "LoadSRML") is {} d) _target.DeclaringType.Methods.Remove(d);
    }

    private static IEnumerable<Instruction> GetLoadingInstructions(ILProcessor proc,ModuleDefinition def) {
        yield return proc.Create(OpCodes.Ldstr, "SRML/Libs");
        yield return proc.Create(OpCodes.Ldstr, "*.dll");
        yield return proc.Create(OpCodes.Ldc_I4_1);

        var method = typeof(Directory).GetMethods().First(x => x.Name == "GetFiles" && x.GetParameters().Length == 3);
        var imported = def.ImportReference(method);
        yield return proc.Create(OpCodes.Call, imported);

        yield return proc.Create(OpCodes.Stloc_0);
        yield return proc.Create(OpCodes.Ldc_I4_0);
        yield return proc.Create(OpCodes.Stloc_1);
        var seventeen = proc.Create(OpCodes.Ldloc_1);
        yield return proc.Create(OpCodes.Br_S, seventeen);
        var eight = proc.Create(OpCodes.Ldloc_0);
        yield return eight;
        yield return proc.Create(OpCodes.Ldloc_1);
        yield return proc.Create(OpCodes.Ldelem_Ref);
        yield return proc.Create(OpCodes.Call, def.ImportReference(typeof(Assembly).GetMethod("LoadFrom",BindingFlags.Public|BindingFlags.Static,Type.DefaultBinder, [typeof(string)],null)));
        yield return proc.Create(OpCodes.Pop);
        yield return proc.Create(OpCodes.Ldloc_1);
        yield return proc.Create(OpCodes.Ldc_I4_1);
        yield return proc.Create(OpCodes.Add);
        yield return proc.Create(OpCodes.Stloc_1);
        yield return seventeen;
        yield return proc.Create(OpCodes.Ldloc_0);
        yield return proc.Create(OpCodes.Ldlen);
        yield return proc.Create(OpCodes.Conv_I4);
        yield return proc.Create(OpCodes.Blt_S, eight);
    }

    private MethodDefinition AddLoadMethod() {
        if (_target.DeclaringType.Methods.FirstOrDefault(x => x.Name == "LoadSRModLoader") is { } d) return d;

        var method = new MethodDefinition("LoadSRModLoader", MethodAttributes.Public|MethodAttributes.Static,_target.Module.TypeSystem.Void);
        method.Body.Variables.Add(new VariableDefinition(_curAssembly.MainModule.ImportReference(typeof(string[]))));
        method.Body.Variables.Add(new VariableDefinition(_curAssembly.MainModule.TypeSystem.Int32));
        var proc = method.Body.GetILProcessor();
        var mainCall = _target.Body.GetILProcessor().Create(OpCodes.Call, _curAssembly.MainModule.ImportReference(_methodToPatchIn));
        if (!_curAssembly.MainModule.TryGetTypeReference("UnityEngine.Debug", out var reference))
            throw new Exception("Couldn't find UnityEngine.Debug");
        var logRef = new MethodReference("Log", _curAssembly.MainModule.TypeSystem.Void, reference);
        logRef.Parameters.Add(new ParameterDefinition(_curAssembly.MainModule.TypeSystem.Object));
        var onFailWrite = proc.Create(OpCodes.Call,logRef);

        if (!_curAssembly.MainModule.TryGetTypeReference("UnityEngine.Application", out var quitreference)) throw new Exception("Couldn't find UnityEngine.Application");

        var appQuit = proc.Create(OpCodes.Call,new MethodReference("Quit",_curAssembly.MainModule.TypeSystem.Void,quitreference));

        var ret = proc.Create(OpCodes.Ret);
        var leave = proc.Create(OpCodes.Leave, ret);
        var mainRet = proc.Create(OpCodes.Ret);

        foreach (var v in GetLoadingInstructions(proc,_curAssembly.MainModule)) proc.Append(v);

        proc.Append(mainCall);
        proc.InsertAfter(mainCall, mainRet);
        proc.InsertAfter(mainRet,onFailWrite);
        proc.InsertAfter(onFailWrite,appQuit);
        proc.InsertAfter(appQuit,leave);
        proc.InsertAfter(leave,ret);

        var handler = new ExceptionHandler(ExceptionHandlerType.Catch) {
            TryStart = method.Body.Instructions.First(),
            TryEnd = onFailWrite,
            HandlerStart = onFailWrite,
            HandlerEnd = ret,
            CatchType = _curAssembly.MainModule.ImportReference(typeof(Exception)),
        };

        method.Body.ExceptionHandlers.Add(handler);
        _target.DeclaringType.Methods.Add(method);
        return method;
    }

    public void Patch() {
        RemoveOldPatch();
        var proc = _target.Body.GetILProcessor();
        proc.InsertBefore(_target.Body.Instructions[0],proc.Create(OpCodes.Call,AddLoadMethod()));
    }

    private void Unpatch() {
        if(_target.Body.Instructions[0].Operand is MethodReference { Name: "LoadSRModLoader" })_target.Body.GetILProcessor().Remove(_target.Body.Instructions[0]);
        if (_target.DeclaringType.Methods.FirstOrDefault(x => x.Name == "LoadSRModLoader") is { } d) _target.DeclaringType.Methods.Remove(d);
        RemoveOldPatch();
    }

    private static bool CheckOrDelete(string path) {
        if (!File.Exists(path)) return true;
        Console.Write($"Found {Path.GetFileName(path)} in target directory! Delete? (y/n): ");
        if (Console.ReadLine() == "y") {
            Console.WriteLine("Deleting and continuing...");
            File.Delete(path);
            return true;
        }
        Console.WriteLine("Aborting...");
        return false;
    }
    
    public void Save() {
        var pathRoot = Path.GetDirectoryName(_filename);
        if (pathRoot == null) return;
        var patchedName = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(_filename) + "_patched.dll");
        var oldName = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(_filename) + "_old.dll");
        if(!CheckOrDelete(patchedName)||!CheckOrDelete(oldName)) throw new Exception("Cannot continue installation while this file exists!");
        _curAssembly.Write(patchedName);
        Dispose();
        if (_filename == null) return;
        File.Move(_filename, oldName);
        File.Move(patchedName, _filename);
    }

    public void Dispose() {
        _curAssembly?.Dispose();
        _methodToPatchIn?.Module?.Assembly?.Dispose();
    }

    public void Uninstall() {
        var pathRoot = Path.GetDirectoryName(_filename);
        if (pathRoot != null) {
            var oldName = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(_filename) + "_old.dll");
            var patchedName = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(_filename) + "_patched.dll");
            if (!File.Exists(oldName)) {
                Console.WriteLine($"Couldn't find old {Path.GetFileName(_filename)}!");
                Console.WriteLine("Attempting forceful uninstallation...");
                Unpatch();
                if (!CheckOrDelete(patchedName)) throw new Exception();
                _curAssembly.Write(patchedName);
                Dispose();
                if (_filename != null) {
                    File.Delete(_filename);
                    File.Move(patchedName, _filename);
                }
            }
            else
            {
                Dispose();
                if (_filename != null) {
                    File.Delete(_filename);
                    Init(oldName);
                    Unpatch();
                    _curAssembly.Write(_filename);
                }
                Dispose();
                File.Delete(oldName);
            }
        }

        Console.WriteLine("Unpatching Successful!");
    }
}