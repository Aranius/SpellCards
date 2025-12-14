using Dnd35.SpellCards.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dnd35.SpellCards.Requests
{
    public sealed class RequestFile
    {
        public List<SpellRequest> Spells { get; set; } = new();
    }
}
