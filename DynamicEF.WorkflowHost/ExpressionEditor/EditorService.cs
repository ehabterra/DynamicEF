using System.Activities.Presentation.View;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.Hosting;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Data;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{

    public class EditorService : IExpressionEditorService
    {

        #region "変数"
        internal TreeNodes IntellisenseData { get; set; }
        internal string EditorKeyWord { get; set; }

        private Dictionary<string, EditorInstance> editorInstances = new Dictionary<string, EditorInstance>();
        #endregion

        public void CloseExpressionEditors()
        {
            var lst = editorInstances.Values.ToList();

            for (var i = 0; i < lst.Count; i++)
            {
                lst[i].LostAggregateFocus -= LostFocus;
                lst[i] = null;
            }
            editorInstances.Clear();
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, System.Collections.Generic.List<ModelItem> variables, string text)
        {
            return CreateExpressionEditorPrivate(assemblies, importedNamespaces, variables, text, null, null);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, System.Collections.Generic.List<ModelItem> variables, string text, System.Type expressionType)
        {
            return CreateExpressionEditorPrivate(assemblies, importedNamespaces, variables, text, expressionType, null);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, System.Collections.Generic.List<ModelItem> variables, string text, System.Type expressionType, System.Windows.Size initialSize)
        {
            return CreateExpressionEditorPrivate(assemblies, importedNamespaces, variables, text, expressionType, initialSize);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, System.Collections.Generic.List<ModelItem> variables, string text, System.Windows.Size initialSize)
        {
            return CreateExpressionEditorPrivate(assemblies, importedNamespaces, variables, text, null, initialSize);
        }


        public void UpdateContext(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces)
        {
        }


        #region "内部メソッド"

        private object _intellisenseLock = new Object();

        private TreeNodes CreateUpdatedIntellisense(IEnumerable<ModelItem> vars)
        {
            TreeNodes result = IntellisenseData;
            lock (_intellisenseLock)
            {
                foreach (var vs in vars)
                {
                    var vsProp = vs.Properties["Name"];
                    if (vsProp == null)
                        continue;
                    var varName = vsProp.ComputedValue;
                    var res = from x in result.Nodes where x.Name == varName.ToString() select x;

                    if (res.FirstOrDefault() == null)
                    {
                        Type sysType = null;
                        var sysTypeProp = vs.Properties["Type"];
                        if (sysTypeProp != null)
                        {
                            sysType = (Type)sysTypeProp.ComputedValue;
                        }
                        TreeNodes newVar = new TreeNodes
                        {
                            Name = varName.ToString(),
                            ItemType = TreeNodes.NodeTypes.Primitive,
                            SystemType = sysType,
                            Description = ""
                        };
                        result.Nodes.Add(newVar);
                    }
                }
            }
            return result;
        }

        private IExpressionEditorInstance CreateExpressionEditorPrivate(AssemblyContextControlItem assemblies,
           ImportedNamespaceContextItem importedNamespaces, IEnumerable<ModelItem> variables
            , string text, System.Type expressionType, Size? initialSize)
        {
            var editor = new EditorInstance()
            {
                IntellisenceList = this.CreateUpdatedIntellisense(variables),
                HighlightWords = this.EditorKeyWord,
                ExpressionType = expressionType,
                Guid = Guid.NewGuid(),
                Text = text ?? ""
            };

            editor.LostAggregateFocus += LostFocus;




            editorInstances.Add(editor.Guid.ToString(), editor);
            return editor;
        }

        private void LostFocus(object sender, EventArgs e)
        {
            var edt = sender as TextBox;
            if (edt != null)
                DesignerView.CommitCommand.Execute(edt.Text);
        }

        #endregion


    }
}