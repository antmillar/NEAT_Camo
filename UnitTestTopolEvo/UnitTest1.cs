using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TopolEvo.NEAT;

namespace UnitTestTopolEvo
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSingletonRandom()
        {
            var actual = Config.globalRandom;

            Console.WriteLine(actual.Next());
            Console.WriteLine(actual.Next());
            
            var expected = Config.globalRandom;

            Console.WriteLine(expected.Next());

            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void TestText()
        {
            var actual = "Blob";
            var expected = "Blob";

            Assert.AreSame(expected, actual);
        }
    }
}
