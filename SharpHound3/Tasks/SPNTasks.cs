﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpHound3.JSON;
using SharpHound3.LdapWrappers;

namespace SharpHound3.Tasks
{
    internal class SPNTasks
    {
        internal static async Task<LdapWrapper> ProcessSPNS(LdapWrapper wrapper)
        {
            if (wrapper is User user)
            {
                await ProcessUserSPNs(user);
            }

            return wrapper;
        }

        private static async Task ProcessUserSPNs(User user)
        {
            var servicePrincipalNames = user.SearchResult.GetPropertyAsArray("serviceprincipalname");
            var domain = user.Domain;
            var resolved = new List<SPNTarget>();

            foreach (var spn in servicePrincipalNames.Where(x => x.StartsWith("mssqlsvc", StringComparison.OrdinalIgnoreCase)))
            {
                int port;
                if (spn.Contains(":"))
                {
                    var success = int.TryParse(spn.Split(':')[1], out port);
                    if (!success)
                        port = 1433;
                }
                else
                {
                    port = 1433;
                }

                var hostSid = await Helpers.TryResolveHostToSid(spn, domain);
                if (hostSid.StartsWith("S-1-5"))
                {
                   resolved.Add(new SPNTarget
                    {
                        ComputerSid = hostSid,
                        Port = port,
                        Service = "SQLAdmin"
                    });
                }
            }

            user.SPNTargets = resolved.Distinct().ToArray();
        }
    }
}
