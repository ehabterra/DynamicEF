using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{
    class TreeNodes
    {

        #region "列挙 Enum"
        /// <summary>ノードのアイテム種別</summary>
        public enum NodeTypes
        {
            Namespace = 0,
            Interface,
            Class,
            Method,
            Property,
            Field,
            Enum,
            ValueType,
            Event,
            Primitive
        }
        #endregion

        #region "変数 Variables"
        private List<TreeNodes> _nodes = new List<TreeNodes>();
        #endregion

        #region "プロパティ Property"
        public string Name { get; set; } = "";
        public string AddStrings { get; set; } = "";
        public NodeTypes ItemType { get; set; }
        public Type SystemType { get; set; }
        public string Description { get; set; }
        public TreeNodes Parent { get; set; }

        private Object _syncLock = new Object();

        public List<TreeNodes> Nodes
        {
            get
            {
                return _nodes;
            }
        }
        #endregion

        #region "メソッド thisthod"
        public void AddNode(TreeNodes target)
        {
            lock (_syncLock)
            {
                _nodes.Add(target);
            }
        }

        public string GetFullPath()
        {
            string result = this.Name;
            if (Parent != null)
            {
                string parentstring = Parent.GetFullPath();
                if (parentstring.Trim() != "")
                    result = parentstring + "." + result;
            }
            return result;
        }

        public TreeNodes SearchNodes(string namePath)
        {
            return this.SearchNodesInprivate(this, namePath);
        }
        private TreeNodes SearchNodesInprivate(TreeNodes targetNodes, string namePath)
        {
            var targetPath = namePath.Split('.');
            bool validPath = false;
            TreeNodes existsNodes = null;

            var validNode = from x in targetNodes.Nodes
                            where x.Name.ToLower() == targetPath[0].ToLower()
                            select x;

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
            if (nextPath.Trim() == "")
                return existsNodes;
            return this.SearchNodesInprivate(existsNodes, nextPath);
        }

        #endregion

    }
}
