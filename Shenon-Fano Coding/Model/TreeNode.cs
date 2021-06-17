namespace Shenon_Fano_Coding.Model
{
    /// <summary>
    /// Вершина дерева фано
    /// </summary>
    class TreeNode
    {
        public TreeNode Left { get; set; }
        public TreeNode Right { get; set; }

        public char? Leaf;

        public TreeNode()
        {
        }

        public TreeNode(char leaf)
        {
            Leaf = leaf;
        }
    }
}