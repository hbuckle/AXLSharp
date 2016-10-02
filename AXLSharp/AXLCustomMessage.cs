using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Xml;
using System.ServiceModel.Description;
using System.ServiceModel;
using System.IO;
using System.ServiceModel.Configuration;

namespace AXLSharp
{
    public class AXLCustomMessage : Message
    {
        private readonly Message message;

        public AXLCustomMessage(Message message)
        {
            this.message = message;
        }
        public override MessageHeaders Headers
        {
            get { return this.message.Headers; }
        }
        public override MessageProperties Properties
        {
            get { return this.message.Properties; }
        }
        public override MessageVersion Version
        {
            get { return this.message.Version; }
        }

        protected override void OnWriteStartBody(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Body", "http://schemas.xmlsoap.org/soap/envelope/");
        }
        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            this.message.WriteBodyContents(writer);
        }
        protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("soapenv", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteAttributeString("xmlns", "ns", null, "http://www.cisco.com/AXL/API/10.5");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
        }
    }

    public class AXLMessageFormatter : IClientMessageFormatter
    {
        private readonly IClientMessageFormatter formatter;

        public AXLMessageFormatter(IClientMessageFormatter formatter)
        {
            this.formatter = formatter;
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = this.formatter.SerializeRequest(messageVersion, parameters);
            return new AXLCustomMessage(message);
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            return this.formatter.DeserializeReply(message, parameters);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AXLFormatMessageAttribute : Attribute, IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription operationDescription,
            BindingParameterCollection bindingParameters)
        { }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            var serializerBehavior = operationDescription.Behaviors.Find<XmlSerializerOperationBehavior>();

            if (clientOperation.Formatter == null)
                ((IOperationBehavior)serializerBehavior).ApplyClientBehavior(operationDescription, clientOperation);

            IClientMessageFormatter innerClientFormatter = clientOperation.Formatter;
            clientOperation.Formatter = new AXLMessageFormatter(innerClientFormatter);
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        { }

        public void Validate(OperationDescription operationDescription) { }
    }

    public class AXLMessageInspector : IClientMessageInspector
    {
        void IClientMessageInspector.AfterReceiveReply(ref Message reply, object correlationState)
        {

        }

        object IClientMessageInspector.BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request = removeNils(request);
            return null;
        }

        private Message removeNils(Message message)
        {
            Message newMessage = null;

            //load the old message into XML
            MessageBuffer msgbuf = message.CreateBufferedCopy(int.MaxValue);

            Message tmpMessage = msgbuf.CreateMessage();
            XmlDictionaryReader xdr = tmpMessage.GetReaderAtBodyContents();

            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(xdr);
            xdr.Close();

            //remove nil elements
            XmlNamespaceManager mgr = new XmlNamespaceManager(xdoc.NameTable);
            mgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            XmlNodeList nullFields = xdoc.SelectNodes("//*[@xsi:nil='true']", mgr);

            if (nullFields != null && nullFields.Count > 0)
            {
                for (int i = 0; i < nullFields.Count; i++)
                {
                    nullFields[i].ParentNode.RemoveChild(nullFields[i]);
                }
            }

            MemoryStream ms = new MemoryStream();
            XmlWriter xw = XmlWriter.Create(ms);
            xdoc.Save(xw);
            xw.Flush();
            xw.Close();

            ms.Position = 0;
            XmlReader xr = XmlReader.Create(ms);

            //create new message from modified XML document
            newMessage = Message.CreateMessage(message.Version, null, xr);
            newMessage.Headers.CopyHeadersFrom(message);
            newMessage.Properties.CopyProperties(message.Properties);

            return newMessage;
        }
    }

    public class AXLEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // No implementation necessary
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new AXLMessageInspector());
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // No implementation necessary
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // No implementation necessary
        }
    }

    public class AXLBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(AXLEndpointBehavior); }
        }

        protected override object CreateBehavior()
        {
            // Create the  endpoint behavior that will insert the message
            // inspector into the client runtime
            return new AXLEndpointBehavior();
        }
    }
}
