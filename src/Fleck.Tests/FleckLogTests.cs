using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class FleckLogTests
    {
        protected int CallCount;
        protected const string Msg = "Test";

        [SetUp]
        public void SetUp()
        {
            CallCount = 0;
            FleckLog.LogAction = (level, s, arg3) => CallCount++;
        }

        [Test]
        public void When_level_is_Debug_Then_CallCount_is_4()
        {
            FleckLog.Level = LogLevel.Debug;
            FleckLog.Debug(Msg);
            FleckLog.Info(Msg);
            FleckLog.Warn(Msg);
            FleckLog.Error(Msg);

            Assert.AreEqual(4, CallCount);
        }

        [Test]
        public void When_level_is_Info_Then_CallCount_is_3()
        {
            FleckLog.Level = LogLevel.Info;
            FleckLog.Debug(Msg);
            FleckLog.Info(Msg);
            FleckLog.Warn(Msg);
            FleckLog.Error(Msg);

            Assert.AreEqual(3, CallCount);
        }

        [Test]
        public void When_level_is_Warn_Then_CallCount_is_2()
        {
            FleckLog.Level = LogLevel.Warn;
            FleckLog.Debug(Msg);
            FleckLog.Info(Msg);
            FleckLog.Warn(Msg);
            FleckLog.Error(Msg);

            Assert.AreEqual(2, CallCount);
        }

        [Test]
        public void When_level_is_Error_Then_CallCount_is_1()
        {
            FleckLog.Level = LogLevel.Error;
            FleckLog.Debug(Msg);
            FleckLog.Info(Msg);
            FleckLog.Warn(Msg);
            FleckLog.Error(Msg);

            Assert.AreEqual(1, CallCount);
        }
    }
}