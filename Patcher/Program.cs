using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

internal static class Program
{
    private static ModuleDefMD _mod;
    private static int _ok, _fail;

    private static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "eh")
        {
            var mm = ModuleDefMD.Load(args[1]);
            var tt = mm.Find("PCL.ModSecret", false);
            var meth = tt.Methods.First(m => m.Name == "ServerSub");
            Console.WriteLine($"maxstack={meth.Body.MaxStack}");
            foreach (var h in meth.Body.ExceptionHandlers)
                Console.WriteLine($"{h.HandlerType}: try IL_{h.TryStart.Offset:X4}-IL_{h.TryEnd.Offset:X4} handler IL_{h.HandlerStart.Offset:X4}-IL_{(h.HandlerEnd == null ? 0 : h.HandlerEnd.Offset):X4} filter IL_{(h.FilterStart == null ? 0 : h.FilterStart.Offset):X4}");
            return 0;
        }
        string input = args.Length > 0 ? args[0] : @"..\..\orig\PCL-original-2.14.0.0.exe";
        string output = args.Length > 1 ? args[1] : @"..\..\out\PCL-patched.exe";

        _mod = ModuleDefMD.Load(input);
        if (Environment.GetEnvironmentVariable("PATCH_DEBUG") == "1")
        {
            foreach (var tn in new[] { "PCL.FormMain", "PCL.PageSetupSystem", "PCL.ModDownload", "PCL.ModSecret" })
            {
                var t = _mod.Find(tn, false);
                Console.WriteLine($"== {tn}: {(t == null ? "null" : t.Methods.Count + " methods")}");
                if (t == null) continue;
                foreach (var mm in t.Methods.Where(x => x.Name.Contains("ctor") || x.Name.Contains("Update") || x.Name.Contains("DlSource") || x.Name.Contains("ServerSub")))
                    Console.WriteLine($"   {mm.Name} params={mm.Parameters.Count} static={mm.IsStatic} ret={mm.ReturnType.TypeName}");
            }
        }
        Console.WriteLine($"loaded: {_mod.Assembly.FullName}, types={_mod.GetTypes().Count()}");

        PatchThemeCheckOne();
        PatchCurrentRank();
        PatchUpdateStart();
        PatchUpdateCheckButton();
        PatchIsLauncherNewest();
        PatchServerSubNotice();
        PatchDlSourceModGet();
        PatchThemeRefresh();
        PatchMainWindowInit();
        PatchUpdateClickHandlers();

        if (Environment.GetEnvironmentVariable("PATCH_TEST") == "1")
        {
            Guard("WaitingMutex(test)", () =>
            {
                var m = M("PCL.Application", "WaitingMutex", 1);
                Stub(m);
            });
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        _mod.Write(output);
        Console.WriteLine($"written: {output}");

        Verify(output);
        Console.WriteLine($"PATCH OK={_ok} FAIL={_fail}");
        return _fail == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- helpers

    private static TypeDef T(string fullName)
    {
        var t = _mod.Find(fullName, false);
        if (t == null) throw new Exception("找不到类型：" + fullName);
        return t;
    }

    private static MethodDef M(string typeName, string methodName, int paramCount = -1)
    {
        var t = T(typeName);
        var list = t.Methods.Where(m => m.Name == methodName).ToList();
        if (paramCount >= 0)
            list = list.Where(m => m.Parameters.Count == paramCount + (m.IsStatic ? 0 : 1)).ToList();
        if (list.Count != 1)
            throw new Exception($"方法定位失败：{typeName}::{methodName} 命中 {list.Count} 个");
        return list[0];
    }

    private static readonly AssemblyRefUser _pclxAsm = new AssemblyRefUser(new AssemblyNameInfo("PCLX"));

    private static IMethod PclxMethod(string name, TypeSig retType, params TypeSig[] args)
    {
        var tr = new TypeRefUser(_mod, "PCLX", "Hooks", _pclxAsm);
        var sig = MethodSig.CreateStatic(retType, args);
        return new MemberRefUser(_mod, name, sig, tr);
    }

    private static void ReplaceBody(MethodDef m, params Instruction[] ins)
    {
        var body = new CilBody();
        body.MaxStack = 8;
        body.KeepOldMaxStack = false;
        foreach (var i in ins) body.Instructions.Add(i);
        m.Body = body;
        _ok++;
        Console.WriteLine($"  [ok] {m.DeclaringType.Name}::{m.Name} 已替换为 {ins.Length} 条指令");
    }

    private static void Stub(MethodDef m)
    {
        ReplaceBody(m, Instruction.Create(OpCodes.Ret));
    }

    private static void Guard(string label, Action a)
    {
        try { a(); }
        catch (Exception ex)
        {
            _fail++;
            Console.WriteLine($"  [FAIL] {label}: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------- patches

    private static void PatchThemeCheckOne()
    {
        Guard("ThemeCheckOne", () =>
        {
            var m = M("PCL.ModSecret", "ThemeCheckOne", 1);
            ReplaceBody(m, Instruction.Create(OpCodes.Ldc_I4_1), Instruction.Create(OpCodes.Ret));
        });
    }

    private static void PatchCurrentRank()
    {
        Guard("CurrentRank", () =>
        {
            var m = M("PCL.ModSecret", "get_CurrentRank", 0);
            Console.WriteLine($"  CurrentRank 返回类型：{m.ReturnType.FullName}");
            ReplaceBody(m, Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)100), Instruction.Create(OpCodes.Ret));
        });
    }

    private static void PatchUpdateStart()
    {
        Guard("UpdateStart", () =>
        {
            var m = M("PCL.ModSecret", "UpdateStart", 2);
            Stub(m);
        });
    }

    private static void PatchUpdateCheckButton()
    {
        Guard("UpdateCheckByButton", () =>
        {
            var m = M("PCL.ModSecret", "UpdateCheckByButton", 0);
            Stub(m);
        });
    }

    private static void PatchIsLauncherNewest()
    {
        Guard("IsLauncherNewest", () =>
        {
            var m = M("PCL.PageSetupSystem", "IsLauncherNewest", 0);
            var nullable = new TypeRefUser(_mod, "System", "Nullable`1", _mod.CorLibTypes.AssemblyRef);
            var gen = new GenericInstSig(new ClassSig(nullable), _mod.CorLibTypes.Boolean);
            var spec = new TypeSpecUser(gen);
            var ctorSig = MethodSig.CreateInstance(_mod.CorLibTypes.Void, _mod.CorLibTypes.Boolean);
            var ctor = new MemberRefUser(_mod, ".ctor", ctorSig, spec);

            var body = new CilBody();
            body.MaxStack = 2;
            body.InitLocals = true;
            var local = new Local(gen);
            body.Variables.Add(local);
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloca_S, local));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            body.Instructions.Add(Instruction.Create(OpCodes.Call, ctor));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            m.Body = body;
            _ok++;
            Console.WriteLine("  [ok] IsLauncherNewest -> true");
        });
    }

    private static void PatchServerSubNotice()
    {
        Guard("ServerSub.notice", () =>
        {
            var m = M("PCL.ModSecret", "ServerSub", 1);
            var body = m.Body;
            body.KeepOldMaxStack = true;
            var ins = body.Instructions;
            // 1) 找到公告循环头：GetEnumerator -> stloc -> br <loopHead>
            Instruction loopHead = null;
            for (int i = 0; i + 2 < ins.Count; i++)
            {
                if (ins[i].OpCode.Code == Code.Callvirt &&
                    (ins[i].Operand?.ToString() ?? "").Contains("GetEnumerator") &&
                    (ins[i + 1].OpCode.Code == Code.Stloc || ins[i + 1].OpCode.Code == Code.Stloc_S) &&
                    (ins[i + 2].OpCode.Code == Code.Br || ins[i + 2].OpCode.Code == Code.Br_S))
                {
                    loopHead = (Instruction)ins[i + 2].Operand;
                    Console.WriteLine($"  公告循环头：ix={ins.IndexOf(loopHead)}");
                    break;
                }
            }
            if (loopHead == null) throw new Exception("未找到公告循环头");

            var candidates = ins.Where(i => (i.OpCode.Code == Code.Ble || i.OpCode.Code == Code.Ble_S ||
                                             i.OpCode.Code == Code.Bgt || i.OpCode.Code == Code.Bgt_S)
                                       && object.ReferenceEquals(i.Operand, loopHead)).ToList();
            Console.WriteLine($"  指向公告循环头的条件跳转：{candidates.Count}");
            if (candidates.Count < 1) throw new Exception("公告分支定位失败");

            // 取最早的那条（公告 id <= 已读编号 时 continue 的判断）
            var ble = candidates.OrderBy(x => ins.IndexOf(x)).First();
            int bi = ins.IndexOf(ble);
            var cmpOperand = ins[bi - 1];
            if (cmpOperand.OpCode.Code < Code.Ldloc_0 || cmpOperand.OpCode.Code > Code.Ldloc_S)
                throw new Exception("公告分支前一条不是 ldloc: " + cmpOperand.OpCode);
            // 把“已读公告编号”换成 int.MaxValue，使 num5 <= num2 恒成立，从而跳过所有公告
            ins[bi - 1] = Instruction.Create(OpCodes.Ldc_I4, int.MaxValue);
            _ok++;
            Console.WriteLine($"  [ok] 公告分支 IL_{ble.Offset:X4} 改为无条件跳转");
        });
    }

    private static void PatchDlSourceModGet()
    {
        Guard("DlSourceModGet", () =>
        {
            var m = M("PCL.ModDownload", "DlSourceModGet", 1);
            var call = PclxMethod("DlSourceModGet", _mod.CorLibTypes.String, _mod.CorLibTypes.String);
            ReplaceBody(m,
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, call),
                Instruction.Create(OpCodes.Ret));
        });
    }

    private static void PatchThemeRefresh()
    {
        Guard("ThemeRefresh", () =>
        {
            var m = M("PCL.ModSecret", "ThemeRefresh", 1);
            var call = PclxMethod("ThemeApply", _mod.CorLibTypes.Void, _mod.CorLibTypes.Int32);
            ReplaceBody(m,
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, call),
                Instruction.Create(OpCodes.Ret));
        });
    }

    private static void PatchMainWindowInit()
    {
        Guard("FormMain..ctor", () =>
        {
            var m = M("PCL.FormMain", ".ctor", 0);
            var body = m.Body;
            body.KeepOldMaxStack = true;
            var ret = body.Instructions.Last(i => i.OpCode.Code == Code.Ret);
            var call = PclxMethod("Init", _mod.CorLibTypes.Void);
            body.Instructions.Insert(body.Instructions.IndexOf(ret), Instruction.Create(OpCodes.Call, call));
            body.OptimizeMacros();
            _ok++;
            Console.WriteLine("  [ok] FormMain..ctor 末尾已注入 Hooks.Init()");
        });
    }

    private static void PatchUpdateClickHandlers()
    {
        Guard("BtnSystemUpdate_Click", () =>
        {
            var m = M("PCL.PageSetupSystem", "BtnSystemUpdate_Click", 2);
            Stub(m);
        });
        Guard("BtnAboutUpdate_Click", () =>
        {
            var t = T("PCL.PageOtherAbout");
            var m = t.Methods.FirstOrDefault(x => x.Name == "BtnAboutUpdate_Click");
            if (m == null) throw new Exception("未找到 BtnAboutUpdate_Click");
            Stub(m);
        });
    }

    // ---------------------------------------------------------------- verify

    private static void Verify(string path)
    {
        using var mod = ModuleDefMD.Load(path);
        void Check(string type, string method, Func<MethodDef, bool> pred, string what)
        {
            var t = mod.Find(type, false);
            var m = t?.Methods.FirstOrDefault(x => x.Name == method);
            bool ok = m != null && pred(m);
            if (ok) Console.WriteLine($"  verify ok: {what}");
            else { _fail++; Console.WriteLine($"  verify FAIL: {what}"); }
        }

        Check("PCL.ModSecret", "ThemeCheckOne", m =>
            m.Body.Instructions.Count == 2 && m.Body.Instructions[0].OpCode.Code == Code.Ldc_I4_1, "ThemeCheckOne=true");
        Check("PCL.ModSecret", "UpdateStart", m =>
            m.Body.Instructions.Count == 1 && m.Body.Instructions[0].OpCode.Code == Code.Ret, "UpdateStart=ret");
        Check("PCL.ModDownload", "DlSourceModGet", m =>
            m.Body.Instructions.Any(i => i.OpCode.Code == Code.Call && (i.Operand?.ToString() ?? "").Contains("PCLX")), "DlSourceModGet->hook");
        Check("PCL.FormMain", ".ctor", m =>
            m.Body.Instructions.Any(i => i.OpCode.Code == Code.Call && (i.Operand?.ToString() ?? "").Contains("PCLX")), "ctor->Init");
    }
}
