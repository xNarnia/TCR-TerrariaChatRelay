using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.Interfaces
{
    public interface IHandlesAttachments
    {
        string HandleAttachment(object obj);
    }
}
