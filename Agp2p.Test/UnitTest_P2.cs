﻿using System;
using Agp2p.Core;
using Agp2p.Linq2SQL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Agp2p.Test
{
    [TestClass]
    public class UnitTest_P2
    {
        private const string UserA = "13535656867";
        private const string UserB = "13590609455";
        private const string CompanyAccount = "CompanyAccount";
        /*
        P2 测试流程：（测试中间人买入债权转让的债权）
        Day 1
            发 4 日标，金额 60000，B 投 25000
        Day 2
            A 投 35000，放款
        Day 3
            A 提现 35000
        Day 4
            公司账号完全接手 A 的提现
        Day 5
        Day 6
            回款
        */

        readonly DateTime realDate = UnitTest_Init.TestStartAt; /* 开始测试前请设置好实际日期 */

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            // 准备好之后注释这行
            throw new InvalidOperationException("1. 备份好数据库；2. 设置实际日期");    
        }

        [TestMethod]
        public void Day01()
        {
            Common.DeltaDay(realDate, 0);

            Common.AutoRepaySimulate();

            // 发 5 日标，金额 60000，B 投 25000
            Common.PublishProject("P2", 5, 60000, 5);
            Common.InvestProject(UserB, "P2", 25000);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day02()
        {
            Common.DeltaDay(realDate, 1);

            Common.AutoRepaySimulate();

            // A 投 35000，放款
            Common.InvestProject(UserA, "P2", 35000);
            Common.ProjectStartRepay("P2");

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day03()
        {
            Common.DeltaDay(realDate, 2);

            Common.AutoRepaySimulate();

            // A 提现 35000
            Common.StaticProjectWithdraw("P2", UserA, 35000);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day04()
        {
            Common.DeltaDay(realDate, 3);

            Common.AutoRepaySimulate();

            // 公司账号接手 B 的提现
            Common.BuyClaim("P2", CompanyAccount, 35000 + 4.86m);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day05()
        {
            Common.DeltaDay(realDate, 4);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day06()
        {
            Common.DeltaDay(realDate, 5);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day07()
        {
            Common.DeltaDay(realDate, 6);

            // 回款，总数应为 41.67
            Common.AutoRepaySimulate();

            // var staticClaimWithdrawCostPercent = ConfigLoader.loadCostConfig().static_withdraw/100;

            Common.AssertWalletDelta(UserA, 4.86m /*- 35000 * staticClaimWithdrawCostPercent*/, 0, 0, 0, 0, 0, 35000, 4.86m, realDate);
            Common.AssertWalletDelta(UserB, 17.36m, 0, 0, 0, 0, 0, 25000, 17.36m, realDate);
            Common.AssertWalletDelta(CompanyAccount, 19.45m, 0, 0, 0, 0, 0, 35000, 24.31m, realDate);
        }

        [TestMethod]
        public void DoCleanUp()
        {
            Common.DoSimpleCleanUp(realDate);
            Common.RestoreDate(realDate);
        }
    }
}
