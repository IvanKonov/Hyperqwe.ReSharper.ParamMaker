using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace NamedParamsMaker
{
    [QuickFix]
    public class CreateNamedParams : QuickFixBase
    {
        private readonly IncorrectArgumentNumberError error;

        public CreateNamedParams(IncorrectArgumentNumberError error)
        {
            this.error = error;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            CSharpElementFactory c_sharp_element_factory = CSharpElementFactory.GetInstance(element: this.error.Reference.GetTreeNode());

            IList<IParameter> parameters;
            if (this.error.Reference.Resolve().DeclaredElement is IMethod declared_element)
            {
                parameters = declared_element.Parameters;
                Replase(
                    solution: solution,
                    c_sharp_element_factory: c_sharp_element_factory,
                    parameters: parameters);
            }
            if (this.error.Reference.Resolve().DeclaredElement is IConstructor constructor)
            {
                parameters = constructor.Parameters;
                Replase(
                    solution: solution,
                    c_sharp_element_factory: c_sharp_element_factory,
                    parameters: parameters);
            }

            return null;
        }

        private void Replase(ISolution solution, CSharpElementFactory c_sharp_element_factory, IList<IParameter> parameters)
        {
            String named_parameters_s = "(";
            foreach (IParameter parameter in parameters)
            {
                named_parameters_s += parameter.ShortName + $": TODO,";
            }
            named_parameters_s = named_parameters_s.Remove(startIndex: named_parameters_s.Length - 1) + ")";
            this.error.Reference.GetTreeNode().GetPsiServices().Transactions.Execute(
                commandName: GetType().Name,
                handler: () =>
                {
                    using (solution.GetComponent<IShellLocks>().UsingWriteLock())
                    {
                        ModificationUtil.AddChild(
                            root: this.error.Reference.GetTreeNode(),
                            child: c_sharp_element_factory.CreateExpressionAsIs(
                                format: named_parameters_s));
                    }
                });
        }

        public override String Text => "Create Named Params";

        public override Boolean IsAvailable(IUserDataHolder cache)
        {
            ResolveResultWithInfo resolve_result_with_info = this.error.Reference.Resolve();
            Boolean is_available = resolve_result_with_info.DeclaredElement is IMethod || resolve_result_with_info.DeclaredElement is IConstructor;
            return is_available;
        }
    }
}