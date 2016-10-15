using System.Collections.Generic;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{

    internal class ComparerName : IComparer<TreeNodes>
    {

        public int Compare(TreeNodes x, TreeNodes y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    return x.Name.CompareTo(y.Name);
                }
            }
        }

    }
}
