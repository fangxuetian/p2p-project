﻿using System;
using System.Linq;
using Agp2p.Core;
using Agp2p.Linq2SQL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Agp2p.Test
{
    [TestClass]
    public class UnitTest_P5_1
    {
        private const string UserA = "13535656867";
        private const string UserB = "13590609455";
        private const string CompanyAccount = "CompanyAccount";
        /*
        P5 测试流程：（测试债权转让，折让的话是否会影响活期）
            Day 1
                发活期标
                发 6 日标，金额 50000，B 投 50000 放款
            Day 2
                B 提现 50000，不要利息；公司账号接手 50000
            Day 3
                A 投资活期 30000
            Day 4
            Day 5
                A 提现活期 20000
            Day 6
            Day 7
                回款
        */

        readonly DateTime TestStartAt = UnitTest_Init.TestStartAt; /* 开始测试前请设置好实际日期 */

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            // 准备好之后注释这行
            throw new InvalidOperationException("1. 备份好数据库；2. 设置实际日期");
        }

        [TestMethod]
        public void Day01()
        {
            Common.DeltaDay(TestStartAt, 0);

            Common.AutoRepaySimulate();

            // 发活期标
            Common.PublishHuoqiProject("HP1");

            // 发 6 日标，金额 50000，B 投 50000，放款
            Common.PublishProject("P5", 6, 50000, 5);
            Common.InvestProject(UserB, "P5", 50000);
            Common.ProjectStartRepay("P5");

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day02()
        {
            Common.DeltaDay(TestStartAt, 1);

            Common.AutoRepaySimulate();

            // B 提现 50000；公司账号接手 50000
            Common.StaticProjectWithdraw("P5", UserB, 50000, 0);
            Common.BuyClaim("P5", CompanyAccount, 50000);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day03()
        {
            Common.DeltaDay(TestStartAt, 2);

            Common.AutoRepaySimulate();

            // A 投资活期 30000
            Common.InvestProject(UserA, "HP1", 30000);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day04()
        {
            Common.DeltaDay(TestStartAt, 3);

            Common.AutoRepaySimulate();
        }


        [TestMethod]
        public void Day05()
        {
            Common.DeltaDay(TestStartAt, 4);

            Common.AutoRepaySimulate();

            // A 提现活期 20000
            Common.HuoqiProjectWithdraw("HP1", UserA, 20000);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day06()
        {
            Common.DeltaDay(TestStartAt, 5);

            Common.AutoRepaySimulate();
        }

        [TestMethod]
        public void Day07()
        {
            Common.DeltaDay(TestStartAt, 6);

            // 回款，总数应为 41.67
            Common.AutoRepaySimulate();

            Common.AssertWalletDelta(UserA, 4.59m, 0, 0, 0, 0, 0, 30000, 4.59m, TestStartAt);
            Common.AssertWalletDelta(UserB, 0m, 0, 0, 0, 0, 0, 50000, 0m, TestStartAt);
            Common.AssertWalletDelta(CompanyAccount, 37.08m, 0, 0, 0, 0, 0, 50000, 41.67m, TestStartAt);
            Assert.AreEqual(0, new Agp2pDataContext().li_projects.Single(p => p.title == "HP1").investment_amount);
        }

        [TestMethod]
        public void DoCleanUp()
        {
            Common.DoSimpleCleanUp(TestStartAt);
            Common.RestoreDate(TestStartAt);
        }
    }
}
