using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Toolbox;
using System.Activities.Statements;
using System.Reflection;
using System.Activities.Presentation.View;
using System.Runtime.Versioning;
using System;
using DynamicEF.DAL;
using Microsoft.Win32;
using System.Threading;
using System.Activities.XamlIntegration;
using System.IO;
using DynamicEF.WorkflowHost.ExpressionEditor;

namespace DynamicEF.WorkflowHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WorkflowDesigner wd;
        private bool creating = true;

        public MainWindow()
        {
            InitializeComponent();

            // Register the metadata  
            RegisterMetadata();

            //Initiate Popup
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                this.CreateIntellisenseList();
                creating = false;
            });

            AddItemsToToolbox();

        }

        private void AddDesigner(string fileName = null)
        {

            //Wait till create Intellisense List 
            while (creating)
            {
                System.Threading.Thread.Sleep(1);
            }

            //Create an instance of WorkflowDesigner class.  
            this.wd = new WorkflowDesigner();

            DesignerConfigurationService configurationService = wd.Context.Services.GetService<DesignerConfigurationService>();
            configurationService.TargetFrameworkName = new FrameworkName(".NETFramework", new System.Version(4, 5));

            configurationService.LoadingFromUntrustedSourceEnabled = true;

            #region Intellisense
            var expEditor = new EditorService()
            {
                IntellisenseData = _inttelisenseList,
                EditorKeyWord = this.CreateKeywords()
            };

            wd.Context.Services.Publish<IExpressionEditorService>(expEditor);
            #endregion


            wd.Context.Items.Subscribe<Selection>(SelectionChanged);

            //Place the designer canvas in the middle column of the grid.  
            Grid.SetColumn(this.wd.View, 1);


            if (fileName == null)
            {
                var activityBuilder = new ActivityBuilder();
                wd.Load(activityBuilder);
                //Load a new Sequence as default.  
                activityBuilder.Implementation = new Sequence();
            }
            else
            {
                this.wd.Load(fileName);
            }

            //// view options
            //var designerView = wd.Context.Services.GetService<DesignerView>();

            //designerView.WorkflowShellBarItemVisibility =
            //    ShellBarItemVisibility.Imports |
            //    ShellBarItemVisibility.MiniMap |
            //    ShellBarItemVisibility.Variables |
            //     ShellBarItemVisibility.Arguments | // <-- Uncomment to show again
            //    ShellBarItemVisibility.Zoom;

            DesignerBorder.Child = wd.View;
            PropertyBorder.Child = wd.PropertyInspectorView;

        }

        private void SelectionChanged(Selection selection)

        {

            var modelItem = selection.PrimarySelection;

            var sb = new StringBuilder();



            while (modelItem != null)

            {

                var displayName = modelItem.Properties["DisplayName"];



                if (displayName != null)

                {

                    if (sb.Length > 0)

                        sb.Insert(0, " - ");

                    sb.Insert(0, displayName.ComputedValue);

                }



                modelItem = modelItem.Parent;

            }



            CurrentActivityName.Text = sb.ToString();

        }

        private void RegisterMetadata()
        {
            DesignerMetadata dm = new DesignerMetadata();
            dm.Register();
        }

        void AddItemsToToolbox()
        {
            string sourceLocation = "";
            string targetLocation = "";
            string location = "";

            ManageAssembly.GetWorkPaths(out sourceLocation, out targetLocation, out location);

            ManageAssembly.CreateAssemblies();

            //New table1 With {.Title = "Ehab"}
            // Add Classes to our domain
            var assemblyNames = ManageAssembly.GetAllDynamicAssemblyNames();
            foreach (var item in assemblyNames)
            {
                var asm = Assembly.LoadFrom(item);
            }
            AppDomain.CurrentDomain.Load("DynamicEF.DAL");

            foreach (var asmembly in AppDomain.CurrentDomain.GetAssemblies())
            {

                var types = asmembly
                    .GetTypes().Where(type =>
                    type.IsVisible &&
                    type.IsPublic &&
                    !type.IsNested &&
                    !type.IsAbstract &&
                    //!type.ContainsGenericParameters &&
                    type.GetConstructors().Where(x => x.GetParameters().Length == 0).FirstOrDefault() != null &&
                    (typeof(Activity).IsAssignableFrom(type)// ||
                   // typeof(IActivityTemplateFactory).IsAssignableFrom(type)
                    )).OrderBy(x => x.FullName).ToList();

                foreach (var type in types)
                {
                    var category = Toolbox.Categories.FirstOrDefault(x => x.CategoryName == type.Namespace);

                    if (category == null)
                    {
                        // add custom activity to toolbox  
                        Toolbox.Categories.Add(new ToolboxCategory(type.Namespace));

                        category = Toolbox.Categories.Last();

                    }
                    category.Add(new ToolboxItemWrapper(type));
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Xaml (*.xaml)|*.xaml";
            if (saveFileDialog.ShowDialog() == true)
                wd.Save(saveFileDialog.FileName);
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var writer = new StringWriter();

            // Run workflow
            wd.Flush();

            Activity dataWorkflow = ActivityXamlServices.Load(new StringReader(wd.Text));

            // Create the WorkflowApplication using the desired
            // workflow definition.
            WorkflowApplication wfApp = new WorkflowApplication(dataWorkflow);

            // Handle the desired lifecycle events.
            wfApp.Completed = delegate (WorkflowApplicationCompletedEventArgs eventarg)
            {
                MessageBox.Show($"Task Finished!\n---- Output ----\n{ writer.ToString() }");
                syncEvent.Set();
            };

            wfApp.Extensions.Add(writer);

            // Start the workflow.
            wfApp.Run();

            // Wait for Completed to arrive and signal that
            // the workflow is complete.
            syncEvent.WaitOne();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialogOpen = new OpenFileDialog();
            dialogOpen.Title = "Open Workflow";
            dialogOpen.Filter = "Workflows (.xaml)|*.xaml";

            if (dialogOpen.ShowDialog() == true)
            {
                using (var file = new StreamReader(dialogOpen.FileName, true))
                {
                    AddDesigner(dialogOpen.FileName);
                }
            }

        }


        /// <summary>
        /// Thanks to https://wfdesignerexpress.codeplex.com/
        /// This code is translated from VB.net
        /// </summary>
        #region "Intellisense"
        private TreeNodes _inttelisenseList = new TreeNodes();

        private void CreateIntellisenseList()
        {
            var wfAsm = Assembly.GetExecutingAssembly();
            var refAsmList = (from x in wfAsm.GetReferencedAssemblies() select Assembly.Load(x)).ToList();
            var typeList = refAsmList.SelectMany(a => (from x in a.GetTypes() where x.IsPublic && x.IsVisible && ((x.Namespace != null) && (!x.Namespace.Contains("DynamicEF"))) select x)).ToList();
            _inttelisenseList.Nodes.Clear();
            foreach (var childAsm in typeList)
            {
                this.AddNode(_inttelisenseList, childAsm.Namespace);
                this.AddTypeNode(_inttelisenseList, childAsm);
            }
            this.AddNode(_inttelisenseList, "New", false);

            this.SortNodes(_inttelisenseList);
        }

        #region " Create Intellisense Node Data"
        private void AddNode(TreeNodes targetNodes, string namePath)
        {
            var targetPath = namePath.Split('.');
            bool validPath = false;
            TreeNodes existsNodes = null;


            var validNode = from x in targetNodes.Nodes where x.Name.ToLower() == targetPath[0].ToLower() select x;


            if ((validNode != null) && (validNode.Count() > 0))
            {
                existsNodes = validNode.FirstOrDefault();
                validPath = true;
            }


            if (!validPath)
            {
                TreeNodes childNodes = new TreeNodes
                {
                    Name = targetPath[0],
                    AddStrings = targetPath[0],
                    ItemType = ExpressionEditor.TreeNodes.NodeTypes.Namespace,
                    Parent = targetNodes,
                    Description = string.Format("Namespace {0}", targetPath[0])
                };
                targetNodes.AddNode(childNodes);


                var nextPath = namePath.Substring(targetPath[0].Length, namePath.Length - targetPath[0].Length);
                if (nextPath.StartsWith("."))
                    nextPath = nextPath.Substring(1, nextPath.Length - 1);
                if (!string.IsNullOrEmpty(nextPath.Trim()))
                    this.AddNode(childNodes, nextPath);
            }
            else
            {
                var nextPath = namePath.Substring(targetPath[0].Length, namePath.Length - targetPath[0].Length);
                if (nextPath.StartsWith("."))
                    nextPath = nextPath.Substring(1, nextPath.Length - 1);
                if (!string.IsNullOrEmpty(nextPath.Trim()))
                    this.AddNode(existsNodes, nextPath);
            }

        }
        private void AddNode(TreeNodes targetNodes, string namePath, bool isNamespace)
        {
            var targetPath = namePath.Split('.');
            bool validPath = false;
            TreeNodes existsNodes = null;

            var validNode = from x in targetNodes.Nodes where x.Name.ToLower() == targetPath[0].ToLower() select x;

            if ((validNode != null) && (validNode.Count() > 0))
            {
                existsNodes = validNode.FirstOrDefault();
                validPath = true;
            }

            if (!validPath)
            {
                TreeNodes childNodes = new TreeNodes
                {
                    Name = targetPath[0],
                    AddStrings = targetPath[0],
                    ItemType = isNamespace ? ExpressionEditor.TreeNodes.NodeTypes.Namespace : ExpressionEditor.TreeNodes.NodeTypes.Primitive,
                    Parent = targetNodes,
                    Description = isNamespace ? string.Format("Namespace {0}", targetPath[0]) : ""
                };
                targetNodes.AddNode(childNodes);

                if (isNamespace)
                {
                    var nextPath = namePath.Substring(targetPath[0].Length, namePath.Length - targetPath[0].Length);
                    if (nextPath.StartsWith("."))
                        nextPath = nextPath.Substring(1, nextPath.Length - 1);
                    if (!string.IsNullOrEmpty(nextPath.Trim()))
                        this.AddNode(childNodes, nextPath);
                }
            }
            else
            {
                if (isNamespace)
                {
                    var nextPath = namePath.Substring(targetPath[0].Length, namePath.Length - targetPath[0].Length);
                    if (nextPath.StartsWith("."))
                        nextPath = nextPath.Substring(1, nextPath.Length - 1);
                    if (!string.IsNullOrEmpty(nextPath.Trim()))
                        this.AddNode(existsNodes, nextPath);
                }
            }
        }

        private void AddTypeNode(TreeNodes targetNodes, Type target)
        {
            if (target.IsAbstract || !target.IsVisible)
                return;

            var typeNamespace = target.Namespace;
            var typeName = target.Name;

            var parentNode = this.SearchNodes(targetNodes, typeNamespace);
            TreeNodes newNodes = new TreeNodes
            {
                Name = typeName,
                AddStrings = typeName,
                Parent = parentNode,
                SystemType = target
            };
            string nodesName = typeName;
            if (target.IsGenericType)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.Class;
                // Case Generic
                if (typeName.Contains("`"))
                {
                    nodesName = typeName.Substring(0, typeName.LastIndexOf("`"));
                    newNodes.AddStrings = nodesName;
                }
                System.Text.StringBuilder paramStrings = new System.Text.StringBuilder();
                int count = 0;
                foreach (var childArg in target.GetGenericArguments())
                {
                    if (count > 0)
                        paramStrings.Append(", ");
                    paramStrings.Append(childArg.Name);
                    count += 1;
                }

                nodesName += "(" + paramStrings.ToString() + ")";
                newNodes.Name = nodesName;
                newNodes.Description = string.Format("Class {0}", newNodes.AddStrings);
            }
            else if (target.IsClass)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.Class;
                newNodes.Description = string.Format("Class {0}", newNodes.AddStrings);
            }
            else if (target.IsEnum)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.Enum;
                newNodes.Description = string.Format("Enum {0}", newNodes.AddStrings);
            }
            else if (target.IsInterface)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.Interface;
                newNodes.Description = string.Format("Interface {0}", newNodes.AddStrings);
            }
            else if (target.IsPrimitive)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.Primitive;
                newNodes.Description = string.Format("{0}", newNodes.AddStrings);
            }
            else if (target.IsValueType)
            {
                newNodes.ItemType = ExpressionEditor.TreeNodes.NodeTypes.ValueType;
                newNodes.Description = string.Format("{0}", newNodes.AddStrings);
            }
            else
            {
                return;
            }

            if (parentNode == null)
            {
                targetNodes.AddNode(newNodes);
            }
            else
            {
                parentNode.AddNode(newNodes);
            }

            this.AddMethodNode(newNodes, target);
            this.AddPropertyNode(newNodes, target);
            this.AddFieldNode(newNodes, target);
            this.AddEventNode(newNodes, target);
            this.AddNestedTypeNode(newNodes, target);

        }

        private void AddMethodNode(TreeNodes targetNodes, Type target)
        {
            System.Threading.Tasks.Parallel.ForEach(target.GetMethods(BindingFlags.Public | BindingFlags.Static |
                                                                   BindingFlags.Instance), (targetmember) =>
        {
            var memberNodes = new TreeNodes()
            {
                Name = targetmember.Name,
                AddStrings = targetmember.Name,
                ItemType = ExpressionEditor.TreeNodes.NodeTypes.Method,
                Parent = targetNodes,
                Description = CreateMethodDescription(targetmember)
            };

            targetNodes.AddNode(memberNodes);
        });
        }

        private void AddPropertyNode(TreeNodes targetNodes, Type target)
        {
            System.Threading.Tasks.Parallel.ForEach(target.GetProperties(BindingFlags.Public | BindingFlags.Static |
                                                           BindingFlags.Instance), (targetmember) =>
                                                           {
                                                               var memberNodes = new TreeNodes()
                                                               {
                                                                   Name = targetmember.Name,
                                                                   AddStrings = targetmember.Name,
                                                                   ItemType = ExpressionEditor.TreeNodes.NodeTypes.Property,
                                                                   Parent = targetNodes,
                                                                   Description = CreatePropertyDescription(targetmember)
                                                               };

                                                               targetNodes.AddNode(memberNodes);
                                                           });
        }

        private void AddFieldNode(TreeNodes targetNodes, Type target)
        {
            System.Threading.Tasks.Parallel.ForEach(target.GetFields(BindingFlags.Public | BindingFlags.Static |
                                                           BindingFlags.Instance), (targetmember) =>
                                                           {
                                                               var memberNodes = new TreeNodes()
                                                               {
                                                                   Name = targetmember.Name,
                                                                   AddStrings = targetmember.Name,
                                                                   ItemType = ExpressionEditor.TreeNodes.NodeTypes.Field,
                                                                   Parent = targetNodes,
                                                                   Description = CreateFieldDescription(targetmember)
                                                               };

                                                               targetNodes.AddNode(memberNodes);
                                                           });
        }

        private void AddEventNode(TreeNodes targetNodes, Type target)
        {
            System.Threading.Tasks.Parallel.ForEach(target.GetEvents(BindingFlags.Public | BindingFlags.Static |
                                                                     BindingFlags.Instance), (targetmember) =>
                                                                     {
                                                                         var memberNodes = new TreeNodes()
                                                                         {
                                                                             Name = targetmember.Name,
                                                                             AddStrings = targetmember.Name,
                                                                             ItemType = ExpressionEditor.TreeNodes.NodeTypes.Event,
                                                                             Parent = targetNodes,
                                                                             Description = CreateEventDescription(targetmember)
                                                                         };

                                                                         targetNodes.AddNode(memberNodes);
                                                                     });
        }

        private void AddNestedTypeNode(TreeNodes targetNodes, Type target)
        {
            System.Threading.Tasks.Parallel.ForEach(target.GetNestedTypes(BindingFlags.Public | BindingFlags.Static |
                                                                     BindingFlags.Instance), (targetmember) =>
                                                                     {
                                                                         var memberNodes = new TreeNodes()
                                                                         {
                                                                             Name = targetmember.Name,
                                                                             AddStrings = targetmember.Name,
                                                                             ItemType = ExpressionEditor.TreeNodes.NodeTypes.Method,
                                                                             Parent = targetNodes
                                                                         };

                                                                         targetNodes.AddNode(memberNodes);
                                                                     });
        }
        #endregion

        private TreeNodes SearchNodes(TreeNodes targetNodes, string namePath)
        {
            var targetPath = namePath.Split('.');
            bool validPath = false;
            TreeNodes existsNodes = null;

            var validNode = from x in targetNodes.Nodes where x.Name.ToLower() == targetPath[0].ToLower() select x;

            if ((validNode != null) && (validNode.Count() > 0))
            {
                existsNodes = validNode.FirstOrDefault();
                validPath = true;
            }

            if (!validPath)
                return targetNodes;

            var nextPath = namePath.Substring(targetPath[0].Length, namePath.Length - targetPath[0].Length);
            if (nextPath.StartsWith("."))
                nextPath = nextPath.Substring(1, nextPath.Length - 1);
            if (string.IsNullOrEmpty(nextPath.Trim()))
                return existsNodes;
            return this.SearchNodes(existsNodes, nextPath);
        }

        private void SortNodes(TreeNodes targetNodes)
        {
            targetNodes.Nodes.Sort(new ComparerName());
            foreach (var childNode in targetNodes.Nodes)
            {
                this.SortNodes(childNode);
            }
        }

        #region "Create Tooltips"

        private string CreateMethodDescription(MethodInfo target)
        {
            StringBuilder desc = new StringBuilder();
            if (target.IsPublic)
                desc.Append("Public ");
            if (target.IsFamily)
                desc.Append("Protected ");
            if (target.IsAssembly)
                desc.Append("Friend ");
            if (target.IsPrivate)
                desc.Append("private ");
            if (target.IsAbstract)
                desc.Append("MustOverride ");
            if (target.IsVirtual && !target.IsFinal)
                desc.Append("Overridable ");
            if (target.IsStatic)
                desc.Append("Shared ");

            if (((!object.ReferenceEquals(target.ReturnType, typeof(void)))))
            {
                desc.Append("Function ");
            }
            else
            {
                desc.Append("Sub ");
            }

            desc.Append(target.Name);
            desc.Append(CreateGenericParameter(target));

            desc.Append("(");
            int paramIndex = 0;
            foreach (var param in target.GetParameters())
            {
                if (paramIndex > 0)
                    desc.Append(", ");
                if (param.IsOptional)
                    desc.Append("Optional ");
                if (param.IsOut)
                {
                    desc.Append("ByRef ");
                }
                else
                {
                    desc.Append("ByVal ");
                }
                desc.Append(param.Name + " As " + param.ParameterType.Name);
                desc.Append(CreateGenericParameter(param.ParameterType));
                if (param.DefaultValue != DBNull.Value)
                {
                    if (param.DefaultValue == null)
                    {
                        desc.Append(" = Nothing");
                    }
                    else
                    {
                        desc.Append(" = " + param.DefaultValue.ToString());
                    }
                }
                paramIndex += 1;
            }
            desc.Append(") ");
            if (target.ReturnType != null)
            {
                desc.Append("As " + target.ReturnType.Name);
                desc.Append(CreateGenericParameter(target.ReturnType));
            }
            return desc.ToString();
        }

        private string CreatePropertyDescription(PropertyInfo target)
        {
            StringBuilder desc = new StringBuilder();

            if (target.CanRead && target.CanWrite)
            {
            }
            else if (target.CanRead)
            {
                desc.Append("ReadOnly ");
            }
            else
            {
                desc.Append("WriteOnly ");
            }
            desc.Append("Property " + target.Name + " As " + target.PropertyType.Name);
            desc.Append(CreateGenericParameter(target.PropertyType));

            return desc.ToString();
        }

        private string CreateFieldDescription(FieldInfo target)
        {
            StringBuilder desc = new StringBuilder();
            if (target.IsPublic)
                desc.Append("Public ");
            if (target.IsPrivate)
                desc.Append("private ");
            if (target.IsStatic)
                desc.Append("Shared ");

            desc.Append(target.Name);
            desc.Append("() ");
            if (target.FieldType != null)
            {
                desc.Append("As " + target.FieldType.Name);
                desc.Append(CreateGenericParameter(target.FieldType));
            }
            return desc.ToString();
        }

        private string CreateEventDescription(EventInfo target)
        {
            StringBuilder desc = new StringBuilder();

            desc.Append(target.Name);
            if (target.EventHandlerType != null)
            {
                desc.Append("As " + target.EventHandlerType.Name);
                if (target.EventHandlerType.IsGenericType)
                {
                    desc.Append(CreateGenericParameter(target.EventHandlerType));
                }
            }
            return desc.ToString();
        }

        private string CreateGenericParameter(MethodInfo target)
        {
            StringBuilder result = new StringBuilder();
            if (target.IsGenericMethod)
            {
                result.Append("(Of ");
                int genIndex = 0;
                foreach (var genParam in target.GetGenericArguments())
                {
                    if (genIndex > 0)
                        result.Append(", ");
                    result.Append(genParam.Name);
                    genIndex += 1;
                }
                result.Append(")");
            }
            return result.ToString();
        }
        private string CreateGenericParameter(Type target)
        {
            StringBuilder result = new StringBuilder();
            if (target.IsGenericType)
            {
                result.Append("(Of ");
                int genIndex = 0;
                foreach (var genParam in target.GetGenericArguments())
                {
                    if (genIndex > 0)
                        result.Append(", ");
                    result.Append(genParam.Name);
                    genIndex += 1;
                }
                result.Append(")");
            }
            return result.ToString();
        }

        #endregion

        #region "Syntax Highlight"
        private string CreateKeywords()
        {
            var words = new StringBuilder();
            words.Append("AddHandler|");
            words.Append("AddressOf|");
            words.Append("Alias|");
            words.Append("And|");
            words.Append("AndAlso|");
            words.Append("As|");
            words.Append("Boolean|");
            words.Append("ByRef|");
            words.Append("Byte|");
            words.Append("ByVal|");
            words.Append("Call|");
            words.Append("Case|");
            words.Append("Catch|");
            words.Append("CBool|");
            words.Append("CByte|");
            words.Append("CChar|");
            words.Append("CDate|");
            words.Append("CDbl|");
            words.Append("CDec|");
            words.Append("Char|");
            words.Append("CInt|");
            words.Append("Class|");
            words.Append("CLng|");
            words.Append("CObj|");
            words.Append("Const|");
            words.Append("Continue|");
            words.Append("CSByte|");
            words.Append("CShort|");
            words.Append("CSng|");
            words.Append("CStr|");
            words.Append("CType|");
            words.Append("CUInt|");
            words.Append("CULng|");
            words.Append("CUShort|");
            words.Append("Date|");
            words.Append("Decimal|");
            words.Append("Declare|");
            words.Append("Default|");
            words.Append("Delegate|");
            words.Append("Dim|");
            words.Append("DirectCast|");
            words.Append("Do|");
            words.Append("Double|");
            words.Append("Each|");
            words.Append("Else|");
            words.Append("ElseIf|");
            words.Append("End|");
            words.Append("EndIf|");
            words.Append("Enum|");
            words.Append("Erase|");
            words.Append("Error|");
            words.Append("Event|");
            words.Append("Exit|");
            words.Append("False|");
            words.Append("Finally|");
            words.Append("For|");
            words.Append("Friend|");
            words.Append("Function|");
            words.Append("Get|");
            words.Append("GetType|");
            words.Append("GetXMLNamespace|");
            words.Append("Global|");
            words.Append("GoSub|");
            words.Append("GoTo|");
            words.Append("Handles|");
            words.Append("If|");
            words.Append("Implements|");
            words.Append("Imports|");
            words.Append("In|");
            words.Append("Inherits|");
            words.Append("Integer|");
            words.Append("Interface|");
            words.Append("Is|");
            words.Append("IsNot|");
            words.Append("Let|");
            words.Append("Lib|");
            words.Append("Like|");
            words.Append("Long|");
            words.Append("Loop|");
            words.Append("Me|");
            words.Append("Mod|");
            words.Append("Module|");
            words.Append("MustInherit|");
            words.Append("MustOverride|");
            words.Append("MyBase|");
            words.Append("MyClass|");
            words.Append("Namespace|");
            words.Append("Narrowing|");
            words.Append("New|");
            words.Append("Next|");
            words.Append("Not|");
            words.Append("Nothing|");
            words.Append("NotInheritable|");
            words.Append("NotOverridable|");
            words.Append("Object|");
            words.Append("Of|");
            words.Append("On|");
            words.Append("Operator|");
            words.Append("Option|");
            words.Append("Optional|");
            words.Append("Or|");
            words.Append("OrElse|");
            words.Append("Out|");
            words.Append("Overloads|");
            words.Append("Overridable|");
            words.Append("Overrides|");
            words.Append("ParamArray|");
            words.Append("Partial|");
            words.Append("private|");
            words.Append("Property|");
            words.Append("Protected|");
            words.Append("Public|");
            words.Append("RaiseEvent|");
            words.Append("ReadOnly|");
            words.Append("ReDim|");
            words.Append("REM|");
            words.Append("RemoveHandler|");
            words.Append("Resume|");
            words.Append("Return|");
            words.Append("SByte|");
            words.Append("Select|");
            words.Append("Set|");
            words.Append("Shadows|");
            words.Append("Shared|");
            words.Append("Short|");
            words.Append("Single|");
            words.Append("Static|");
            words.Append("Step|");
            words.Append("Stop|");
            words.Append("String|");
            words.Append("Structure|");
            words.Append("Sub|");
            words.Append("SyncLock|");
            words.Append("Then|");
            words.Append("Throw|");
            words.Append("To|");
            words.Append("True|");
            words.Append("Try|");
            words.Append("TryCast|");
            words.Append("TypeOf|");
            words.Append("UInteger|");
            words.Append("ULong|");
            words.Append("UShort|");
            words.Append("Using|");
            words.Append("Variant|");
            words.Append("Wend|");
            words.Append("While|");
            words.Append("Widening|");
            words.Append("With|");
            words.Append("WithEvents|");
            words.Append("WriteOnly|");
            words.Append("Xor|");
            words.Append("#Const|");
            words.Append("#Else|");
            words.Append("#ElseIf|");
            words.Append("#End|");
            words.Append("#If");
            return words.ToString();
        }
        #endregion

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Add the WFF Designer  
            AddDesigner();
        }
    }
}
