﻿
using System;
using System.Collections.Generic;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;


namespace DBOps.MySql
{
    /// <summary>
    /// An implementation of ScriptExecutor that executes against a MySql database.
    /// </summary>
    public class MySqlScriptExecutor: DbUp.MySql.MySqlScriptExecutor
    {
        /// <summary>
        /// Initializes an instance of the <see cref="MySqlScriptExecutor"/> class.
        /// </summary>
        /// <param name="connectionManagerFactory"></param>
        /// <param name="log">The logging mechanism.</param>
        /// <param name="schema">The schema that contains the table.</param>
        /// <param name="variablesEnabled">Function that returns <c>true</c> if variables should be replaced, <c>false</c> otherwise.</param>
        /// <param name="scriptPreprocessors">Script Preprocessors in addition to variable substitution</param>
        /// <param name="journalFactory">Database journal</param>
        public MySqlScriptExecutor(Func<IConnectionManager> connectionManagerFactory, Func<IUpgradeLog> log, string schema, Func<bool> variablesEnabled,
            IEnumerable<DbUp.Engine.IScriptPreprocessor> scriptPreprocessors, Func<DbUp.Engine.IJournal> journalFactory)
            : base(connectionManagerFactory, log, schema, variablesEnabled, scriptPreprocessors, journalFactory)
        {
        }
        protected override void ExecuteCommandsWithinExceptionHandler(int index, DbUp.Engine.SqlScript script, Action executeCommand)
        {
            SqlScript s = (SqlScript)script;
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                base.ExecuteCommandsWithinExceptionHandler(index, s, executeCommand);
            }
            catch
            {
                throw;
            }
            finally
            {
                stopWatch.Stop();
                s.SetExecutionTime(stopWatch.ElapsedMilliseconds);
            }
        }
    }
}
