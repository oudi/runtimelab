// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.JitInterface;

namespace ILCompiler
{
    public class RyuJitCompilationBuilder : CompilationBuilder
    {
        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private KeyValuePair<string, string>[] _ryujitOptions = Array.Empty<KeyValuePair<string, string>>();
        private ILProvider _ilProvider = new NativeAotILProvider();
        private ProfileDataManager _profileDataManager;
        private string _jitPath;

        protected RyuJitCompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup group, NodeMangler mangler)
            : base(context, group, new NativeAotNameMangler(mangler))
        {
        }

        public RyuJitCompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup group)
            : base(context, group,
                  new NativeAotNameMangler(context.Target.IsWindows ? (NodeMangler)new WindowsNodeMangler(context.Target) : (NodeMangler)new UnixNodeMangler()))
        {
        }

        public RyuJitCompilationBuilder UseProfileData(IEnumerable<string> mibcFiles)
        {
            _profileDataManager = new ProfileDataManager(mibcFiles, _context);
            return this;
        }

        public RyuJitCompilationBuilder UseJitPath(string jitPath)
        {
            _jitPath = jitPath;
            return this;
        }

        public override CompilationBuilder UseBackendOptions(IEnumerable<string> options)
        {
            var builder = default(ArrayBuilder<KeyValuePair<string, string>>);

            foreach (string param in options)
            {
                int indexOfEquals = param.IndexOf('=');

                // We're skipping bad parameters without reporting.
                // This is not a mainstream feature that would need to be friendly.
                // Besides, to really validate this, we would also need to check that the config name is known.
                if (indexOfEquals < 1)
                    continue;

                string name = param.Substring(0, indexOfEquals);
                string value = param.Substring(indexOfEquals + 1);

                builder.Add(new KeyValuePair<string, string>(name, value));
            }

            _ryujitOptions = builder.ToArray();

            return this;
        }

        protected IEnumerable<KeyValuePair<string, string>> GetBackendOptions() => _ryujitOptions;

        public override CompilationBuilder UseILProvider(ILProvider ilProvider)
        {
            _ilProvider = ilProvider;
            return this;
        }

        protected override ILProvider GetILProvider()
        {
            return _ilProvider;
        }

        public sealed override ICompilation ToCompilation()
        {
            ArrayBuilder<CorJitFlag> jitFlagBuilder = default(ArrayBuilder<CorJitFlag>);

            switch (_optimizationMode)
            {
                case OptimizationMode.None:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_DEBUG_CODE);
                    break;

                case OptimizationMode.PreferSize:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_SIZE_OPT);
                    break;

                case OptimizationMode.PreferSpeed:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_SPEED_OPT);
                    break;

                default:
                    // Not setting a flag results in BLENDED_CODE.
                    break;
            }

            if (_optimizationMode != OptimizationMode.None && _profileDataManager != null)
            {
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_BBOPT);
            }

            // Do not bother with debug information if the debug info provider never gives anything.
            if (!(_debugInformationProvider is NullDebugInformationProvider))
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_DEBUG_INFO);

            RyuJitCompilationOptions options = 0;
            if ((_mitigationOptions & SecurityMitigationOptions.ControlFlowGuardAnnotations) != 0)
            {
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_ENABLE_CFG);
                options |= RyuJitCompilationOptions.ControlFlowGuardAnnotations;
            }

            if (_useDwarf5)
                options |= RyuJitCompilationOptions.UseDwarf5;

            if (_resilient)
                options |= RyuJitCompilationOptions.UseResilience;

<<<<<<< HEAD
            JitConfigProvider.Initialize(_context.Target, jitFlagBuilder.ToArray(), _ryujitOptions, _jitPath);
            return CreateCompilation(options);
        }

        protected virtual RyuJitCompilation CreateCompilation(RyuJitCompilationOptions options)
        {
            var factory = new RyuJitNodeFactory(_context, _compilationGroup, _metadataManager, _interopStubManager, _nameMangler, _vtableSliceProvider, _dictionaryLayoutProvider, _inlinedThreadStatics, GetPreinitializationManager(), _devirtualizationManager);
=======
            ObjectDataInterner interner = _methodBodyFolding ? new ObjectDataInterner() : ObjectDataInterner.Null;

            var factory = new RyuJitNodeFactory(_context, _compilationGroup, _metadataManager, _interopStubManager, _nameMangler, _vtableSliceProvider, _dictionaryLayoutProvider, _inlinedThreadStatics, GetPreinitializationManager(), _devirtualizationManager, interner);
>>>>>>> 61050ae9b4e38dce8a9f7fe2d1a11eea5fa92b99

            DependencyAnalyzerBase<NodeFactory> graph = CreateDependencyGraph(factory, new ObjectNode.ObjectNodeComparer(CompilerComparer.Instance));
            return new RyuJitCompilation(graph, factory, _compilationRoots, _ilProvider, _debugInformationProvider, _logger, _inliningPolicy ?? _compilationGroup, _instructionSetSupport, _profileDataManager, _methodImportationErrorProvider, _readOnlyFieldPolicy, options, _parallelism);
        }
    }
}
