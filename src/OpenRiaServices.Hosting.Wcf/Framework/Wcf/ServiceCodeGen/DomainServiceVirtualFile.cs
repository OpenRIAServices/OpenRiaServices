using System;
using System.IO;
using System.Text;
using System.Web.Hosting;

namespace OpenRiaServices.Hosting.Wcf
{
    internal sealed class DomainServiceVirtualFile : VirtualFile
    {
        private readonly Type domainServiceType;

        public DomainServiceVirtualFile(Type domainServiceType, string virtualPath)
            : base(virtualPath)
        {
            this.domainServiceType = domainServiceType;
        }

        public override Stream Open()
        {
            MemoryStream contentStream = null;

            try
            {
                contentStream = new MemoryStream();
                StreamWriter contentWriter = new StreamWriter(contentStream, Encoding.Unicode);

                contentWriter.WriteLine("<%@ ServiceHost Factory=\"{0}\" Service=\"{1}\" %>",
                    typeof(DomainServiceHostFactory).AssemblyQualifiedName,
                    this.domainServiceType.AssemblyQualifiedName);
                contentWriter.Flush();
                contentStream.Position = 0;
            }
            catch
            {
                contentStream.Close();
                throw;
            }

            return contentStream;
        }
    }
}
