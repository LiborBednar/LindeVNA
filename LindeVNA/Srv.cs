using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDisposable = System.IDisposable;
using FrameworkElement = System.Windows.FrameworkElement;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Debug = System.Diagnostics.Debug;

namespace LindeVNA
{
    public class WaitCursorIndicator : IDisposable
    {
        FrameworkElement Onwer;
        Cursor Previous;

        public WaitCursorIndicator(FrameworkElement owner)
        {
            this.Onwer = owner;
            if (owner == null) return;
            Previous = owner.Cursor;
            owner.Cursor = Cursors.Wait;
        }

        void IDisposable.Dispose()
        {
            if (this.Onwer == null) return;
            this.Onwer.Cursor = Previous;
        }
    } 
}