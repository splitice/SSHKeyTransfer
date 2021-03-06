﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Renci.SshNet.Messages;
using Renci.SshNet.Messages.Authentication;
using Renci.SshNet.Common;
using System.Threading.Tasks;

namespace Renci.SshNet
{
    public partial  class KeyboardInteractiveAuthenticationMethod : AuthenticationMethod
    {
        partial void ExecuteThread(Action action)
        {
            Task.Factory.StartNew(action);
        }
    }
}
