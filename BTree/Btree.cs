using System;
using System.Collections.Generic;

namespace BTree
{
    /// <summary>
    /// Дерево
    /// </summary>
    internal class Btree
    {
        /// <summary>
        /// Корень
        /// </summary>
        public BTreeNode Root;

        /// <summary>
        /// Фактор
        /// </summary>
        public int T;

        private Btree(int t)
        {
            Root = null;
            T = t;
        }

        // function to search a key in this tree
        public BTreeNode Search(int key)
        {
            if (Root == null)
                return null;
            else
                return Root.Search(key);
        }

        public void Insert(int key)
        {
            if (Root == null)
            {
                Root = new BTreeNode(T, false);
            }

            Root.Insert(key);
        }
    }

    /// <summary>
    /// Вершина
    /// </summary>
    public class BTreeNode
    {
        private List<int> _keys; // Ключи
        private int _t; // минимальный фактор
        private List<BTreeNode> _childs; // An array of child pointers
        private bool _leaf; // Is true when node is leaf. Otherwise false

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
        /// <param name="leaf"></param>
        public BTreeNode(int t, bool leaf = true)
        {
            _t = t;
            _leaf = leaf;
            _keys = new List<int>(2 * t - 1);
            _childs = new List<BTreeNode>(2 * t);
        }

        // A function to search a key in the subtree rooted with this node.
        public BTreeNode Search(int key)
        {
            // Находим ключ который больше или равен нашему
            int i = 0;
            while (i < _keys.Count && key > _keys[i])
                i++;

            // Если ключ равен возвращаем нашу вершину
            if (_keys[i] == key)
                return this;

            // Если не нашли и это лист, возвращаем null
            if (_leaf == true)
                return null;

            // Идем к правельному ребенку
            return _childs[i].Search(key);
        }

        public void Insert(int key)
        {
            //TODO: Условие оставления ключа на этом уровне
            if (_keys.Count == 0)
            {
                _keys.Add(key);
                _childs.Add(new BTreeNode(_t));
                _childs.Add(new BTreeNode(_t));
                _leaf = false;
            }

            int i = 0;
            while (i < _keys.Count && key > _keys[i])
                i++;

            if(_keys[i] > key)
                _childs[i].Insert(key);

            if(_keys[i]<=key)
                _childs[i+1].Insert(key);

        }
    }
}