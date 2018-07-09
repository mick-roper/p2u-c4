using Structurizr;
using Structurizr.Api;
using Structurizr.Documentation;
using System;

namespace C4
{
    class Program
    {
        private const long WORKSPACE_ID = 39608;
        private const string API_KEY = "69dd3992-503c-41c4-a655-438147ee8e07";
        private const string API_SECRET = "e84db31a-b4d2-4405-b01d-4606a21f028f";

        static void Main(string[] args)
        {
            var workspace = new Workspace("P2U - Spinal Tap", "NHS data interaction system");
            var model = workspace.Model;
            model.Enterprise = new Enterprise("Pharmacy 2 U");

            #region software systems
            var nhsSpine = model.AddSoftwareSystem(Location.External, "NHS Spine", "SOR for all patient information");
            var gemPlus = model.AddSoftwareSystem(Location.External, "GEM+", "Smart card authentication software");
            var p2uSpinalTap = model.AddSoftwareSystem(Location.Internal, "P2U Spinal Tap", "Receives, produces and routes messages bound to/from the NHS spine");
            var p2uSubsystems = model.AddSoftwareSystem(Location.Internal, "P2U internal subsystem", "Performs P2U business processes (dispensery, order tracking, shipping, etc.)");
            var p2uDataWarehouse = model.AddSoftwareSystem(Location.Internal, "P2U Data Warehouse", "Long term data storage");

            p2uSpinalTap.Uses(nhsSpine, "Makes requests to", "REST over HTTPS", InteractionStyle.Asynchronous);
            p2uSpinalTap.Uses(p2uDataWarehouse, "Sends updates to", "AMQP", InteractionStyle.Asynchronous);
            p2uSpinalTap.Uses(p2uSubsystems, "Gets updates from", "AMQP", InteractionStyle.Asynchronous);
            p2uSpinalTap.Uses(gemPlus, "Authenticates with");

            p2uSubsystems.Uses(p2uSpinalTap, "Gets messages from", "AMQP", InteractionStyle.Asynchronous);
            #endregion

            #region containers
            var p2uSpinalTapServer = p2uSpinalTap.AddContainer("Spinal Tap Server 1", "A server", "Windows Server 2016");
            var p2uSpinalTapDbServer = p2uSpinalTap.AddContainer("Spinal Tap Database Server", "A server", "MS SQL 2017");
            var p2uAmqpHost = p2uSpinalTap.AddContainer("AMQP host", "A server", "Windows Server 2016");
            #endregion

            #region Components
            var restClient = p2uSpinalTapServer.AddComponent("REST client", "Makes REST requests");
            var messageParser = p2uSpinalTapServer.AddComponent("Message parser", "Converts P2U messages to/from HL7 messages");
            var dataSink = p2uSpinalTapServer.AddComponent("Data Sink", "Receives and routes messages");
            var dataPump = p2uSpinalTapServer.AddComponent("Data Pump", "Emits messages");
            var securityGateway = p2uSpinalTapServer.AddComponent("GEM+ Bridge", "Provides access to the GEM+ authentication service");

            var stateDb = p2uSpinalTapDbServer.AddComponent("Spinal Tap State Database", "Holds state information about spinal tap interactions");

            restClient.Uses(nhsSpine, "Makes requests to");
            restClient.Uses(messageParser, "Relays response to");

            messageParser.Uses(dataSink, "processes then (optionally) routes the message");
            messageParser.Uses(restClient, "sends formatted messages to");

            dataSink.Uses(stateDb, "Updates");
            dataSink.Uses(p2uAmqpHost, "Routes messages to");

            #endregion

            var systemView = workspace.Views.CreateSystemContextView(p2uSpinalTap, "System Context", "1,000 ft view");
            systemView.Add(nhsSpine);
            systemView.Add(p2uSubsystems);
            systemView.Add(gemPlus);

            var containerView = workspace.Views.

            UploadWorkspace(workspace);
        }

        static void UploadWorkspace(Workspace workspace)
        {
            var client = new StructurizrClient(API_KEY, API_SECRET);
            client.PutWorkspace(WORKSPACE_ID, workspace);
        }
    }
}
