using Hyperbrid.UIX.Tools.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public interface IEditCommand
    {
        string Name { get; }

        void DoRedo();

        void DoUndo();
    }


    public class IndexChange
    {
        public IndexChange(int before, int after)
        {
            BeforeIndex = before;
            AfterIndex = after;
        }

        public int BeforeIndex;
        public int AfterIndex;
    }
}
