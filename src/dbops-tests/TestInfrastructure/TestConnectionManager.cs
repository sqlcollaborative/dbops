﻿using System;
using System.Collections.Generic;
using System.Data;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;

namespace DBOps.Tests.TestInfrastructure
{
    public class TestConnectionManager : DatabaseConnectionManager
    {
        public TestConnectionManager(IDbConnection connection, bool startUpgrade = false) : base(l => connection)
        {
            if (startUpgrade)
                OperationStarting(new ConsoleUpgradeLog(), new List<DbUp.Engine.SqlScript>());
        }

        public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
        {
            return new[] {scriptContents};
        }
    }
}