using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hash
{
    public class CustomHashTable
    {
        private List<HashNode> _table;

        public int Capacity => _table.Capacity;



        public CustomHashTable(int length = 256)
        {
            _table = new List<HashNode>(length);
            for (int i = 0; i < Capacity; i++)
            {
                _table.Add(null);
            }
        }

        public void Add(string s)
        {
            var h = Hash(s);
            var node = _table[h];
            if (node == null)
            {
                _table[h] = new HashNode(){Key = h, Value = s};
            }
            else
            {
                while (node.NextNode != null) node = node.NextNode;
                node.NextNode = new HashNode(){Key = h, Value = s};
            }
        }

        public int Find(string s)
        {
            var h = Hash(s);
            var node = _table[h];
            if (node != null)
            {
                if (node.Value == s) return h;
                while (node.NextNode != null)
                {
                    node = node.NextNode;
                    if (node.Value == s) return h;
                }
            }

            return -1;
        }

        private int Hash(string s)
        {
            var summ = 0;
            foreach (var c in s)
            {
                summ += c;
            }

            return summ % _table.Capacity;
        }
    }
}