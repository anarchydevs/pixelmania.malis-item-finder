using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MalisItemFinder
{
    public class AssemblyHelper
    {
        public static Assembly ResolveAssemblyOnCurrentDomain(object sender, ResolveEventArgs args)
        {
            var requestedAssembly = new AssemblyName(args.Name);
            var assembly = default(Assembly);

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyOnCurrentDomain;

            try
            {
                assembly = Assembly.Load(requestedAssembly.Name);
            }
            catch
            {
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyOnCurrentDomain;

            return assembly;
        }
    }
}