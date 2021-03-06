﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class Issue_2481 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enlist_the_receive_in_the_dtc_tx()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<DTCEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.HandlerInvoked)
                .Run();

            Assert.False(context.CanEnlistPromotable, "There should exists a DTC tx");
        }

        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public bool CanEnlistPromotable { get; set; }
        }

        public class DTCEndpoint : EndpointConfigurationBuilder
        {
            public DTCEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport()
                        .Transactions(TransportTransactionMode.TransactionScope);
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.CanEnlistPromotable = Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager());
                    Context.HandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }
        }


        public class MyMessage : ICommand
        {
        }
    }
}