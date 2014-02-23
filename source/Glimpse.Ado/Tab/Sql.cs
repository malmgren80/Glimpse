﻿using System.Collections.Generic;
using System.Linq;
using Glimpse.Ado.Message;
using Glimpse.Ado.Model;
using Glimpse.Ado.Tab.Support;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;
using System.Diagnostics;
using System.Reflection;
using Glimpse.Core.Framework;
using System;

namespace Glimpse.Ado.Tab
{
    public class SQL : TabBase, ITabSetup, IKey, ITabLayout, IDocumentation, ILayoutControl
    {
        private static readonly object Layout =
            TabLayout.Create()
                .Cell("SQL Statistics", TabLayout.Create().Row(r => 
                {
                    r.Cell("connectionCount").WidthInPixels(150).WithTitle("# Connections");
                    r.Cell("queryCount").WidthInPixels(150).WithTitle("# Queries");
                    r.Cell("transactionCount").WidthInPixels(150).WithTitle("# Transactions");
                    r.Cell("queryExecutionTime").WidthInPixels(250).Suffix(" ms").Class("mono").WithTitle("Total query execution time");
                    r.Cell("connectionOpenTime").Suffix(" ms").Class("mono").WithTitle("Total connection open time");
                }))
                .Cell("Queries", TabLayout.Create().Row(r => {
                    r.Cell(0).DisablePreview().SetLayout(TabLayout.Create().Row(x =>
                            x.Cell(0).SpanColumns(7).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPixels(165);
                                y.Cell(1);
                            }))).Row(x =>
                            {
                                x.Cell(1).WidthInPixels(55);
                                x.Cell(2).AsCode(CodeType.Sql).DisablePreview();
                                x.Cell(3).WidthInPercent(25).DisablePreview();
                                x.Cell(4).WidthInPixels(60);
                                x.Cell(5).WidthInPixels(85).Suffix(" ms").Class("mono");
                                x.Cell(6).WidthInPixels(95).Prefix("T+ ").Suffix(" ms").Class("mono");
                                x.Cell(7).WidthInPixels(45);
                            }).Row(x =>
                            x.Cell(9).SpanColumns(7).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPercent(20);
                                y.Cell(1).Class("mono").DisablePreview();
                            }))).Row(x =>
                            x.Cell(10).SpanColumns(7).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).Class("mono");
                            }))).Row(x =>
                            x.Cell(8).SpanColumns(7).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPixels(165);
                                y.Cell(1);
                            }))));
                    r.Cell(1).WidthInPixels(75).Suffix(" ms").Class("mono");
                }))
                .Build();

        public override string Name
        {
            get { return "SQL"; }
        }

        public string Key
        {
            get { return "glimpse_sql"; }
        }

        public string DocumentationUri
        {
            get { return "http://getglimpse.com/Help/SQL-Tab"; }
        }

        public bool KeysHeadings
        {
            get { return true; }
        }

        public void Setup(ITabSetupContext context)
        {
            context.PersistMessages<AdoMessage>();
        }

        public object GetLayout()
        {
            return Layout;
        }

        public override object GetData(ITabContext context)
        {
            var sanitizer = new CommandSanitizer();
            var messages = context.GetMessages<AdoMessage>().ToList();
            var aggregator = new MessageAggregator(messages);
            var queryMetadata = aggregator.Aggregate();

            if (queryMetadata == null)
            {
                return null;
            }

            var connections = new List<object[]> { new object[] { "Commands per Connection", "Duration" } };

            foreach (var connection in queryMetadata.Connections.Values)
            {
                if (connection.Commands.Count == 0 && connection.Transactions.Count == 0)
                {
                    continue;
                }

                var commands = new List<object[]> { new object[] { "Transaction Start", "Ordinal", "Command", "Parameters", "Records", "Duration", "Offset", "Async", "Transaction End", "Errors", "Stack" } };
                var commandCount = 1;
                foreach (var command in connection.Commands.Values)
                {
                    // Transaction Start
                    List<object[]> headTransaction = null;
                    if (command.HeadTransaction != null)
                    {
                        headTransaction = new List<object[]> { new object[] { "\t▼ Transaction - Started", "Isolation Level - " + command.HeadTransaction.IsolationLevel } };
                        if (!command.HeadTransaction.Committed.HasValue)
                        {
                            headTransaction.Add(new object[] { string.Empty, "Transaction was never completed", "error" });
                        }
                    }

                    // Transaction Finish
                    List<object[]> tailTransaction = null;
                    if (command.TailTransaction != null)
                    {
                        tailTransaction = new List<object[]> { new object[] { "\t▲ Transaction - Finished", "Status - " + (command.TailTransaction.Committed.GetValueOrDefault() ? "Committed" : "Rollbacked") } };
                    }

                    // Parameters
                    List<object[]> parameters = null;
                    if (command.Parameters.Count > 0)
                    {
                        parameters = new List<object[]> { new object[] { "Name", "Value", "Type", "Size" } };
                        foreach (var parameter in command.Parameters)
                        {
                            parameters.Add(new[] { parameter.Name, parameter.Value, parameter.Type, parameter.Size });
                        }
                    }

                    // Exception
                    List<object[]> errors = null;
                    if (command.Exception != null)
                    {
                        var exception = command.Exception.GetBaseException();
                        var exceptionName = command.Exception != exception ? command.Exception.Message + ": " + exception.Message : exception.Message;

                        errors = new List<object[]> { new object[] { "Error", "Stack" }, new object[] { exceptionName, exception.StackTrace } };
                    }

                    // Stacktrace
                    List<object[]> stackTrace = null;
                    if (command.StackTrace != null && command.Exception == null)
                    {
                        string stackTraceText = StackFilter.GetFilteredStackTrace(command.StackTrace);
                        if (!string.IsNullOrEmpty(stackTraceText))
                        {
                            stackTrace = new List<object[]> { new object[] { "Stack" }, new object[] { stackTraceText } };
                        }
                    }

                    // Commands
                    var records = command.RecordsAffected == null || command.RecordsAffected < 0 ? command.TotalRecords : command.RecordsAffected;

                    var status = errors != null ? "error" : (command.IsDuplicate ? "warn" : string.Empty);
                    commands.Add(new object[] { headTransaction, string.Format("{0}{1}", command.HasTransaction ? "\t\t\t" : string.Empty, commandCount++), sanitizer.Process(command.Command, command.Parameters), parameters, records, command.Duration, command.Offset, command.IsAsync, tailTransaction, errors, stackTrace, status });
                }

                connections.Add(new[] { commands, connection.Duration.HasValue ? (object)connection.Duration.Value : null });
            }

            if (connections.Count > 1)
            {
                SqlStatistics sqlStatistics = SqlStatisticsCalculator.Caluculate(queryMetadata);

                return new Dictionary<string, object>
                {
                    { "SQL Statistics", new object[] { new { sqlStatistics.ConnectionCount, sqlStatistics.QueryCount, sqlStatistics.TransactionCount, sqlStatistics.QueryExecutionTime, sqlStatistics.ConnectionOpenTime } } }, 
                    { "Queries", connections }
                };
            }

            return null;
        }

        private StackTraceFilter _filter;
        private StackTraceFilter StackFilter
        {
            get { return _filter == null ? (_filter = new ReflectionBlackListStackFrameFilter()) : _filter; }
        }

        public class StackTraceFilter
        {
            private readonly HashSet<string> _excludedTypes = new System.Collections.Generic.HashSet<string>();
            private readonly HashSet<string> _excludedMethods = new System.Collections.Generic.HashSet<string>();
            private readonly HashSet<string> _excludedAssemblies = new System.Collections.Generic.HashSet<string>();

            private IEnumerable<StackFrameInfo> GetFilteredMethods(StackTrace trace)
            {
                if (trace == null)
                {
                    yield break;
                }

                foreach(var frame in trace.GetFrames())
                {
                    var method = frame.GetMethod();
                    
                    if (ShouldExcludeMethod(method) || 
                        ShouldExcludeType(method) ||
                        ShouldExcludeAssembly(method.Module.Assembly))
                    {
                        continue;
                    }

                    yield return new StackFrameInfo(frame, method);
                }
            }

            public string GetFilteredStackTrace(StackTrace trace)
            {
                var methodNames =
                    (from method in GetFilteredMethods(trace)
                     select method.ToString()).ToArray();

                return string.Join(Environment.NewLine, methodNames);
            }

            protected virtual bool ShouldExcludeMethod(MethodBase method)
            {
                return _excludedMethods.Contains(method.Name);
            }

            protected virtual bool ShouldExcludeType(MethodBase method)
            {
                var t = method.DeclaringType;

                while (t != null)
                {
                    if (_excludedTypes.Contains(t.Name))
                    { 
                        return true;
                    }

                    t = t.DeclaringType;
                }

                return false;
            }

            protected virtual bool ShouldExcludeAssembly(Assembly assembly)
            {
                return _excludedAssemblies.Contains(assembly.GetName().Name);
            }

            public void ExcludeType(string type)
            {
                _excludedTypes.Add(type);
            }

            public void ExcludeMethod(string methodName)
            {
                _excludedMethods.Add(methodName);
            }

            public void ExcludeAssembly(string assemblyName)
            {
                _excludedAssemblies.Add(assemblyName);
            }

            private class StackFrameInfo
            {
                private const string AnonymousMethodDescription = " --- () => {...}";

                private readonly StackFrame _frame;
                private readonly MethodBase _method;

                public StackFrameInfo(StackFrame frame, MethodBase method)
                {
                    _frame = frame;
                    _method = method;
                }

                private bool IsAnonymousMethod
                {
                    get
                    {
                        // Don't know if this will work in all cases...
                        return _frame.ToString().StartsWith("<") || 
                            _method.Name.Equals("lambda_method", StringComparison.OrdinalIgnoreCase);
                    }
                }

                private string FullMethodName
                {
                    get
                    {
                        string methodName = string.Format("{0}()", _method.Name);
                        if (_method.DeclaringType == null)
                        {
                            return methodName;
                        
                        }
                        return string.Format("{0}.{1}", _method.DeclaringType.FullName, methodName);
                    }
                }

                public override string ToString()
                {
                    try
                    {
                        return IsAnonymousMethod ? string.Concat(FullMethodName, AnonymousMethodDescription) : FullMethodName;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        throw e;
                    }
                }
            }
        }

        public class ReflectionBlackListStackFrameFilter : StackTraceFilter
        {
            protected override bool ShouldExcludeMethod(MethodBase method)
            {
                return false;
            }

            protected override bool ShouldExcludeType(MethodBase method)
            {
                return false;
            }

            protected override bool ShouldExcludeAssembly(Assembly assembly)
            {
                if (assembly.GetName().Name.ToUpperInvariant().StartsWith("GLIMPSE"))
                { 
                    return true;
                }

                return ReflectionBlackList.IsBlackListed(assembly);
            }
        }
    }
}