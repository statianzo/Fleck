using System.Diagnostics;
using NUnit.Framework;

namespace Fleck.Tests
{
    [SetUpFixture]
    public class AssemblyTests
    {
        [SetUp]
        public void SetUp()
        {
            FleckLog.LogAction = (level, message, ex) => Debug.WriteLine("[{0}]{1}: {2}", level, message, ex);
        }
    }
}