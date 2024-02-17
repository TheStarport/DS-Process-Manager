using System;
using System.Collections.Generic;
using System.Text;

namespace DSProcessManager
{
    public interface FLHookListener
    {
        /// <summary>
        /// Receive an event from the flhook command socket.
        /// </summary>
        /// <param name="type">The command type from the keys[0] field</param>
        /// <param name="keys">An array of parameter keys.</param>
        /// <param name="values">An array of parameter values.</param>
        /// <param name="eventLine">The unparsed event line</param>
        void ReceiveFLHookEvent(string type, string[] keys, string[] values, string eventLine);
    }
}
