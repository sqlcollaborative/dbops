﻿using System;
using System.Data;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBOps.Extensions
{
    /// <summary>
    /// An child class of <see cref="DbUp.Postgresql.PostgresqlTableJournal"/> that adds custom fields to the 
    /// SchemaVersions table.
    /// </summary>
    public class PostgresqlTableJournal: DbUp.Postgresql.PostgresqlTableJournal
    {
        bool journalExists;
        Version tableVersion = new Version("2.0");
        public PostgresqlTableJournal(Func<IConnectionManager> connectionManager, Func<IUpgradeLog> logger, string schema, string table)
            : base(connectionManager, logger, schema, table)
        {
        }
        public override void EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
        {
            var tableExists = DoesTableExist(dbCommandFactory);
            if (!journalExists && !tableExists)
            {
                if (tableExists)
                {
                    var currentTableVersion = GetTableVersion(dbCommandFactory);
                    if (currentTableVersion < tableVersion)
                    {
                        Log().WriteInformation("Upgrading schema version table...");
                        foreach (var sql in AlterSchemaTableSql(currentTableVersion))
                        {
                            var command = dbCommandFactory();
                            command.CommandText = sql;
                            command.CommandType = CommandType.Text;
                            command.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    Log().WriteInformation(string.Format("Creating the {0} table", FqSchemaTableName));
                    using (var command = GetCreateTableCommand(dbCommandFactory))
                    {
                        command.ExecuteNonQuery();
                    }

                    Log().WriteInformation(string.Format("The {0} table has been created", FqSchemaTableName));

                    OnTableCreated(dbCommandFactory);
                }
            }

            journalExists = true;
        }
        protected string GetInsertJournalEntrySql(string @scriptName, string @applied, string @checksum, string @executionTime)
        {
            return $"insert into {FqSchemaTableName} (ScriptName, Applied, CheckSum, AppliedBy, ExecutionTime) values ({@scriptName}, {@applied}, {@checksum}, current_user, {@executionTime})";
        }

        protected override string CreateSchemaTableSql(string quotedPrimaryKeyName)
        {
            return
$@"CREATE TABLE {FqSchemaTableName}
(
    schemaversionsid serial NOT NULL,
    scriptname character varying(255) NOT NULL,
    applied timestamp without time zone NOT NULL,
    checksum character varying(255),
    appliedby character varying(255),
    executiontime bigint,
    CONSTRAINT {quotedPrimaryKeyName} PRIMARY KEY (schemaversionsid)
)";
        }

        protected new IDbCommand GetInsertScriptCommand(Func<IDbCommand> dbCommandFactory, DbUp.Engine.SqlScript script)
        {
            SqlScript s = (SqlScript)script;
            var command = dbCommandFactory();

            var scriptNameParam = command.CreateParameter();
            scriptNameParam.ParameterName = "scriptName";
            scriptNameParam.Value = s.Name;
            command.Parameters.Add(scriptNameParam);

            var appliedParam = command.CreateParameter();
            appliedParam.ParameterName = "applied";
            appliedParam.Value = DateTime.Now;
            command.Parameters.Add(appliedParam);

            var checksumParam = command.CreateParameter();
            checksumParam.ParameterName = "checksum";
            checksumParam.Value = Helpers.CreateMD5(s.Contents);
            command.Parameters.Add(checksumParam);

            var etParam = command.CreateParameter();
            etParam.ParameterName = "executionTime";
            etParam.Value = s.ExecutionTime;
            command.Parameters.Add(etParam);


            command.CommandText = GetInsertJournalEntrySql("@scriptName", "@applied", "@checksum", "@executionTime");
            command.CommandType = CommandType.Text;
            return command;
        }

        protected List<String> AlterSchemaTableSql(Version currentTableVersion)
        {
            var sqlList = new List<String>();
            if (currentTableVersion.Major == 1)
            {
                sqlList.Add($@"alter table {FqSchemaTableName} add 
    checksum character varying(255),
    appliedby character varying(255),
    executiontime bigint");
            }
            return sqlList;

        }

        protected Version GetTableVersion(Func<IDbCommand> dbCommandFactory)
        {
            var columns = new List<string>();
            using (var command = dbCommandFactory())
            {
                command.CommandText = GetTableVersionSql();
                command.CommandType = CommandType.Text;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        columns.Add((string)reader[0]);
                }
            }
            if (columns.Contains("Checksum", StringComparer.OrdinalIgnoreCase))
            {
                return new Version("2.0");
            }
            else
            {
                return new Version("1.0");
            }
        }

        protected string GetTableVersionSql()
        {
            return string.Format("select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{0}'", UnquotedSchemaTableName) +
                (string.IsNullOrEmpty(SchemaTableSchema) ? "" : string.Format(" and TABLE_SCHEMA = '{0}'", SchemaTableSchema));
        }



        /// <summary>
        /// Records a database upgrade for a database specified in a given connection string.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="dbCommandFactory"></param>
        public override void StoreExecutedScript(DbUp.Engine.SqlScript script, Func<IDbCommand> dbCommandFactory)
        {
            SqlScript s = (SqlScript)script;
            EnsureTableExistsAndIsLatestVersion(dbCommandFactory);
            using (var command = GetInsertScriptCommand(dbCommandFactory, s))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}