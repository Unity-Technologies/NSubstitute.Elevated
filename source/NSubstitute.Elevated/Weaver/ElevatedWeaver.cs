using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;
using NiceIO;
using Unity.Core;

namespace NSubstitute.Elevated.Weaver
{
    [Flags]
    public enum PatchOptions
    {
        PatchTestAssembly   = 1 << 0,   // typically we don't want to patch the test assembly itself, only the systems under test
        SkipPeVerify        = 1 << 1,   // maybe flip this bit the other way when we get a really solid weaver (peverify has an obvious perf cost)
    }

    public static class ElevatedWeaver
    {
        const string k_PatchBackupExtension = ".orig";

        public static string GetPatchBackupPathFor(string path)
        => path + k_PatchBackupExtension;

        public static IReadOnlyCollection<PatchResult> PatchAllDependentAssemblies(NPath testAssemblyPath, PatchOptions patchOptions)
        {
            // TODO: ensure we do not have any assemblies that we want to patch already loaded
            // (this will require the separate in-memory patching ability)

            // this dll has types we're going to be injecting, so ensure it is in the same folder
            //var targetWeaverDll

            var toProcess = new List<NPath> { testAssemblyPath.FileMustExist() };
            var patchResults = new Dictionary<string, PatchResult>(StringComparer.OrdinalIgnoreCase);
            var mockInjector = new MockInjector();

            EnsureMockTypesAssemblyInFolder(testAssemblyPath.Parent);

            for (var toProcessIndex = 0; toProcessIndex < toProcess.Count; ++toProcessIndex)
            {
                var assemblyToPatchPath = toProcess[toProcessIndex];

                // as we accumulate dependencies recursively, we will probably get some duplicates we can early-out on
                if (patchResults.ContainsKey(assemblyToPatchPath))
                    continue;

                using (var assemblyToPatch = AssemblyDefinition.ReadAssembly(assemblyToPatchPath))
                {
                    GatherReferences(assemblyToPatchPath, assemblyToPatch);

                    var patchResult = TryPatch(assemblyToPatchPath, assemblyToPatch);
                    patchResults.Add(assemblyToPatchPath, patchResult);
                }
            }

            void GatherReferences(NPath assemblyToPatchPath, AssemblyDefinition assemblyToPatch)
            {
                foreach (var referencedAssembly in assemblyToPatch.Modules.SelectMany(m => m.AssemblyReferences))
                {
                    // only patch dll's we "own", that are in the same folder as the test assembly
                    var referencedAssemblyPath = assemblyToPatchPath.Parent.Combine(referencedAssembly.Name + ".dll");

                    if (referencedAssemblyPath.FileExists())
                        toProcess.Add(referencedAssemblyPath);
                    else if (!patchResults.ContainsKey(referencedAssembly.Name))
                        patchResults.Add(referencedAssembly.Name, new PatchResult(referencedAssembly.Name, null, PatchState.IgnoredForeignAssembly));
                }
            }

            PatchResult TryPatch(NPath assemblyToPatchPath, AssemblyDefinition assemblyToPatch)
            {
                var alreadyPatched = MockInjector.IsPatched(assemblyToPatch);
                var cannotPatch = assemblyToPatch.Name.HasPublicKey;

                if (assemblyToPatchPath == testAssemblyPath && (patchOptions & PatchOptions.PatchTestAssembly) == 0)
                {
                    if (alreadyPatched)
                        throw new Exception("Unexpected already-patched test assembly, yet we want PatchTestAssembly.No");
                    if (cannotPatch)
                        throw new Exception("Cannot patch an assembly with a strong name");
                    return new PatchResult(assemblyToPatchPath, null, PatchState.IgnoredTestAssembly);
                }

                if (alreadyPatched)
                    return new PatchResult(assemblyToPatchPath, null, PatchState.AlreadyPatched);
                if (cannotPatch)
                    return new PatchResult(assemblyToPatchPath, null, PatchState.IgnoredForeignAssembly);
                
                return Patch(assemblyToPatchPath, assemblyToPatch);
            }

            PatchResult Patch(NPath assemblyToPatchPath, AssemblyDefinition assemblyToPatch)
            {
                mockInjector.Patch(assemblyToPatch);

                // atomic write of file with backup
                // TODO: skip backup if existing file already patched. want the .orig to only be the unpatched file.

                // write to tmp and release the lock
                var tmpPath = assemblyToPatchPath.ChangeExtension(".tmp");
                tmpPath.DeleteIfExists();
                assemblyToPatch.Write(tmpPath); // $$$ , new WriterParameters { WriteSymbols = true }); see https://github.com/jbevain/cecil/issues/421
                assemblyToPatch.Dispose();

                if ((patchOptions & PatchOptions.SkipPeVerify) == 0)
                    PeVerify.Verify(tmpPath);

                // move the actual file to backup, and move the tmp to actual
                var backupPath = GetPatchBackupPathFor(assemblyToPatchPath);
                File.Replace(tmpPath, assemblyToPatchPath, backupPath);

                // TODO: move pdb file too

                return new PatchResult(assemblyToPatchPath, backupPath, PatchState.Patched);
            }

            return patchResults.Values;
        }

        static void EnsureMockTypesAssemblyInFolder(NPath targetFolder)
        {
            // ensure that our assembly with the mock types is discoverable by putting in the same folder as the dll that is having its types
            // injected into it. we could mess with the assembly resolver to avoid this, but that won't solve the issue for appdomains and
            // other environments that we don't control, like peverify.

            var mockTypesSrcPath = new NPath(MockInjector.MockTypesAssembly.Location);
            var mockTypesDstPath = targetFolder.Combine(mockTypesSrcPath.FileName);

            if (mockTypesSrcPath != mockTypesDstPath)
                mockTypesSrcPath.Copy(mockTypesDstPath);
        }

        public static IReadOnlyCollection<PatchResult> PatchAssemblies(
            [NotNull] List<string> testAssemblyPaths)
        {
            var testAssemblyPath = testAssemblyPaths[0];
            var testAssemblyFolder = Path.GetDirectoryName(testAssemblyPath);
            if (testAssemblyFolder.IsNullOrEmpty())
                throw new Exception("Unable to find folder for test assembly");
            testAssemblyFolder = Path.GetFullPath(testAssemblyFolder);

            // scope
            {
                var thisAssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (thisAssemblyFolder.IsNullOrEmpty())
                    throw new Exception("Can only patch assemblies on disk");
                thisAssemblyFolder = Path.GetFullPath(thisAssemblyFolder);

                // keep things really simple, at least for now
                if (string.Compare(testAssemblyFolder, thisAssemblyFolder, StringComparison.OrdinalIgnoreCase) != 0)
                    throw new Exception("All assemblies must be in the same folder");
            }

            var nsubElevatedPath = Path.Combine(testAssemblyFolder, "NSubstitute.Elevated.dll");
            var mockInjector = new MockInjector();

            foreach (var assemblyPath in testAssemblyPaths)
            {
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
                mockInjector.Patch(assemblyDefinition);
                // atomic write of file with backup
                var tmpPath = assemblyPath.Split(new[] { ".dll" }, StringSplitOptions.None)[0] + ".tmp";
                File.Delete(tmpPath);
                assemblyDefinition.Write(tmpPath);//$$$$, new WriterParameters { WriteSymbols = true });  // getting exception, haven't looked into it yet
                assemblyDefinition.Dispose();
                /*var originalPath = GetPatchBackupPathFor(assemblyToPatchPath);
                File.Replace(tmpPath, assemblyToPatchPath, originalPath);*/
                // $$$ TODO: move pdb file too
            }

            return null;
        }
    }

    public enum PatchState
    {
        GeneralFailure,            // something else went wrong
        IgnoredTestAssembly,       // don't patch the test assembly itself, as we're requiring that to always be separate from the systems under test
        IgnoredForeignAssembly,    // don't want to patch things that are not "ours"
        //AlreadyPatchedOld,       // assy already patched against an older set of tooling TODO: implement
        AlreadyPatched,            // assy already patched against current tooling
        Patched,                   // assy patched and old one backed up
    }

    public struct PatchResult
    {
        public string Path;
        public string OriginalPath;
        public PatchState PatchState;

        [DebuggerStepThrough]
        public PatchResult(string path, string originalPath, PatchState patchState)
        {
            Path = path;
            OriginalPath = originalPath;
            PatchState = patchState;
        }
    }
}
