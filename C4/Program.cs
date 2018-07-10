using Structurizr;
using Structurizr.Api;
using Structurizr.Documentation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace C4
{
    class Program
    {
        private const long WORKSPACE_ID = 39615;
        private const string API_KEY = "1b24e557-9cec-489c-866c-776bcbf6715c";
        private const string API_SECRET = "0c062628-2bad-429d-af16-47343e76922b";

        static void Main(string[] args)
        {
            var workspace = new Workspace("P2U Spinal Tap", "NHS Integration Platform");
            var model = workspace.Model;
            model.Enterprise = new Enterprise("Pharmacy 2 U");

            #region software systems
            var nhsSpine = model.AddSoftwareSystem(Location.External, "NHS Spine", "SOR for all patient information");
            var gemPlus = model.AddSoftwareSystem(Location.External, "GEM+", "Smart card authentication software");
            var p2uSpinalTap = model.AddSoftwareSystem(Location.Internal, "P2U Spinal Tap", "Receives, produces and routes messages bound to/from the NHS spine");
            var p2uSubsystems = model.AddSoftwareSystem(Location.Internal, "P2U internal subsystem", "Performs P2U business processes (dispensery, order tracking, shipping, etc.)");

            var customer = model.AddPerson("Customer", "A person using the service");

            customer.Uses(p2uSubsystems, "Utilises");

            nhsSpine.AddTags("external");
            gemPlus.AddTags("external");

            p2uSpinalTap.Uses(nhsSpine, "Makes requests to", "REST over HTTPS", InteractionStyle.Asynchronous);
            p2uSpinalTap.Uses(p2uSubsystems, "Gets updates from", "AMQP", InteractionStyle.Asynchronous);
            p2uSpinalTap.Uses(gemPlus, "Authenticates with");

            p2uSubsystems.Uses(p2uSpinalTap, "Gets messages from", "AMQP", InteractionStyle.Asynchronous);
            #endregion

            #region containers
            var p2uSpinalTapServer = p2uSpinalTap.AddContainer("Spinal Tap Server", "Server", "Windows Server 2016");
            var p2uSpinalTapDbServer = p2uSpinalTap.AddContainer("Spinal Tap Database Server", "Server", "MS SQL 2017");
            var p2uSpinalTapRevProxy = p2uSpinalTap.AddContainer("Reverse Proxy Server", "Server", "Windows Server|Linux");
            var p2uAmqpHost = p2uSpinalTap.AddContainer("AMQP host", "Server", "Windows Server 2016|Ubuntu 16|Debian");

            p2uSpinalTapRevProxy.Model.AddDeploymentNode("Rev Proxy Server", "Reverse proxy server", "Linux", 3);
            p2uSpinalTapServer.Model.AddDeploymentNode("Spinal Tap Server", "A stateless server", "Windows Server", 3);

            p2uSpinalTapRevProxy.Uses(p2uSpinalTapServer, "Sends requests to", "HTTP");

            p2uSpinalTapServer.Uses(p2uSpinalTapDbServer, "Gets/sets state from", "T-SQL");
            p2uSpinalTapServer.Uses(p2uAmqpHost, "Pushes/Pull messages from", "AMQP");

            p2uAmqpHost.AddTags("service-bus");
            #endregion

            #region Components
            var revProxyService = p2uSpinalTapRevProxy.AddComponent("Nginx Service", "Reverse proxy");
            var p2uSpinalTapService = p2uSpinalTapServer.AddComponent("Spinal Tap Service", "Handles interractions with NHS spine");
            var stateDb = p2uSpinalTapDbServer.AddComponent("Spinal Tap State Database", "Holds state information about spinal tap interactions");

            revProxyService.Uses(p2uSpinalTapService, "Routes requests to");

            p2uSpinalTapService.Uses(nhsSpine, "Makes requests to");
            p2uSpinalTapService.Uses(stateDb, "Stores state in");
            p2uSpinalTapService.Uses(p2uAmqpHost, "Pushes/pulls messages to/from");
            p2uSpinalTapService.Uses(gemPlus, "Authenticates with");
            #endregion

            var systemView = workspace.Views.CreateSystemContextView(p2uSpinalTap, "System Context", "1,000 ft view");
            systemView.Add(customer);
            systemView.Add(nhsSpine);
            systemView.Add(p2uSubsystems);
            systemView.Add(gemPlus);
            systemView.PaperSize = PaperSize.A4_Landscape;

            var containerView = workspace.Views.CreateContainerView(p2uSpinalTap, "Containers", "Physical layout of the solution");
            containerView.AddAllContainers();

            var componentView = workspace.Views.CreateComponentView(p2uSpinalTapServer, "Spinal Tap Server Components", "The core components used to interract with NHS spine");
            componentView.Add(gemPlus);
            componentView.Add(nhsSpine);
            componentView.Add(p2uAmqpHost);
            componentView.Add(stateDb);
            AddComponents(p2uSpinalTapServer.Components, componentView);
            componentView.PaperSize = PaperSize.A4_Landscape;

            var styles = workspace.Views.Configuration.Styles;
            styles.Add(new ElementStyle(Tags.Person) { Shape = Shape.Person });
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Shape = Shape.Box });
            styles.Add(new ElementStyle(Tags.Container) { Shape = Shape.Folder });
            styles.Add(new ElementStyle(Tags.ContainerInstance) { Shape = Shape.Hexagon });
            styles.Add(new ElementStyle(Tags.Component) { Shape = Shape.RoundedBox });
            styles.Add(new ElementStyle("service-bus") { Shape = Shape.Pipe });
            styles.Add(new ElementStyle("external") { Shape = Shape.Hexagon });

            UploadWorkspace(workspace);
        }

        private static void AddComponents(IEnumerable<Component> components, ComponentView componentView)
        {
            foreach (var component in components)
            {
                componentView.Add(component);
            }
        }

        static void UploadWorkspace(Workspace workspace)
        {
            var client = new StructurizrClient(API_KEY, API_SECRET);
            client.PutWorkspace(WORKSPACE_ID, workspace);
        }
    }
}
