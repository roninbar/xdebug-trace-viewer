using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xdebug_trace_viewer
{
    static class Stack
    {
        internal static void Push<T>(this List<T> stack, T item)
        {
            stack.Insert(0, item);
        }

        internal static T Pop<T>(this List<T> stack)
        {
            T head = stack[0];
            stack.RemoveAt(0);
            return head;
        }
    }
}
