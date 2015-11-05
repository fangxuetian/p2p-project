﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Agp2p.Common;
using Agp2p.Core;
using Agp2p.Linq2SQL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Agp2p.Test
{
    /// <summary>
    /// UnitTest1 的摘要说明
    /// </summary>
    [TestClass]
    public class UnitTest_Transaction
    {
        public UnitTest_Transaction()
        {
            //
            //TODO: 在此处添加构造函数逻辑
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region 附加测试特性
        //
        // 编写测试时，可以使用以下附加特性:
        //
        // 在运行类中的第一个测试之前使用 ClassInitialize 运行代码
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // 在类中的所有测试都已运行之后使用 ClassCleanup 运行代码
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 在运行每个测试之前，使用 TestInitialize 来运行代码
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 在每个测试运行完之后，使用 TestCleanup 来运行代码
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        // 在运行每个测试之前，使用 TestInitialize 来运行代码
        [TestInitialize]
        public void MyTestInitialize()
        {
            // 创建测试用户
            // 为其充值
            // 充值确认
            // 创建测试项目，设置期限、期数和还款类型

        }

        private static readonly string str = "server=192.168.5.98;uid=sa;pwd=Zxcvbnm,;database=agrh;";

        private static string GetFriendlyUserName(dt_users user)
        {
            return string.IsNullOrWhiteSpace(user.real_name)
                ? user.user_name
                : string.Format("{0}({1})", user.user_name, user.real_name);
        }

        private static string GetUserPassword(dt_users user)
        {
            return DESEncrypt.Decrypt(user.password, user.salt);
        }

        [TestMethod]
        public void CleanAllProjectAndTransactionRecord()
        {
            var context = new Agp2pDataContext(str);
            var now = DateTime.Now;

            context.li_projects.ForEach(p =>
            {
                context.li_repayment_tasks.DeleteAllOnSubmit(p.li_repayment_tasks);

                p.li_project_transactions.ForEach(ptr =>
                {
                    context.li_invitations.DeleteAllOnSubmit(ptr.li_invitations);
                    context.li_wallet_histories.DeleteAllOnSubmit(ptr.li_wallet_histories);
                });
                context.li_project_transactions.DeleteAllOnSubmit(p.li_project_transactions);
            });
            context.li_projects.DeleteAllOnSubmit(context.li_projects);

            context.dt_users.ForEach(u =>
            {
                var wallet = u.li_wallets;
                wallet.idle_money = 0;
                wallet.investing_money = 0;
                wallet.unused_money = 0;
                wallet.locked_money = 0;
                wallet.profiting_money = 0;
                wallet.last_update_time = now;
                wallet.total_charge = 0;
                wallet.total_withdraw = 0;
                wallet.total_investment = 0;
                wallet.total_profit = 0;

                u.li_bank_transactions.ForEach(chargeRecord =>
                {
                    context.li_wallet_histories.DeleteAllOnSubmit(chargeRecord.li_wallet_histories);
                });
                context.li_bank_transactions.DeleteAllOnSubmit(u.li_bank_transactions);

                u.li_bank_accounts.ForEach(account =>
                {
                    account.li_bank_transactions.ForEach(withdrawRecord =>
                    {
                        context.li_wallet_histories.DeleteAllOnSubmit(withdrawRecord.li_wallet_histories);
                    });
                    context.li_bank_transactions.DeleteAllOnSubmit(account.li_bank_transactions);
                });

                u.li_activity_transactions.ForEach(atr =>
                {
                    context.li_wallet_histories.DeleteAllOnSubmit(atr.li_wallet_histories);
                });
                context.li_activity_transactions.DeleteAllOnSubmit(u.li_activity_transactions);
            });

            context.dt_manager_log.DeleteAllOnSubmit(context.dt_manager_log);
            //context.SubmitChanges();
        }

        private static IEnumerable<Tuple<string, DateTime, DateTime>> GenMountlyTimeSpan(DateTime startTime, DateTime endTime)
        {
            for (var i = startTime; i <= endTime; i = i.AddMonths(1))
            {
                var name = i.Month == 1 ? i.ToString("yyyy年MM月") : i.ToString("MM月");
                var startSpan = new DateTime(i.Year, i.Month, 1);
                yield return
                    new Tuple<string, DateTime, DateTime>(name, startSpan, startSpan.AddMonths(1));
            }
        }

        [TestMethod]
        public void TestEval()
        {
            var HelloWorld = EvalProvider.CreateEvalMethod<int, string>(@"return ""Hello world "" + arg;");

            Console.WriteLine(HelloWorld(42));
        }
    }
}
