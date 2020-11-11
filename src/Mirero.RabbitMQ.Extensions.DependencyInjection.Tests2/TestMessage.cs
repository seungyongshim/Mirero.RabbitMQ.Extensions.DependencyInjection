using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests2
{
    public class TestMessage
    {
        public TestMessage(string value) => Value = value;

        public string Value { get; }
    }

    public class TestMessage2
    {
        public TestMessage2(string value) => Value = value;

        public string Value { get;  }
    }
}
