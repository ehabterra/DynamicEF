using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{
    public partial class IntellisSencePopUp
    {
        public IntellisSencePopUp()
        {
            InitializeComponent();
        }

        internal TreeNodes SelectedItem
        {
            get { return lbxIntelliSence.SelectedItem as TreeNodes; }
            set { lbxIntelliSence.SelectedItem = value; }
        }

        internal int SelectedIndex
        {
            get { return lbxIntelliSence.SelectedIndex; }
            set
            {
                if (value >= lbxIntelliSence.Items.Count || value < -1)
                {
                    return;
                }
                lbxIntelliSence.SelectedIndex = value;
                lbxIntelliSence.ScrollIntoView(lbxIntelliSence.SelectedItem);
            }
        }

        internal int ItemsCount
        {
            get { return lbxIntelliSence.Items.Count; }
        }

        internal event KeyEventHandler ListBoxKeyDown;
        internal event MouseButtonEventHandler ListBoxItemDoubleClick;

        protected void OnListBoxItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItemDoubleClick(sender, e);
        }

        protected void OnListBoxPrevieKeyDown(object sender, KeyEventArgs e)
        {
            ListBoxKeyDown(sender, e);
        }

    }
}