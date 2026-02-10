#if NET

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using OpenRiaServices;
using OpenRiaServices.Server;

namespace People
{
    /// <summary>
    /// A simple test <see cref="CodeProcessor"/>, does not alter the codegen output in any way.
    /// </summary>
    /// <remarks>This is used to verify the generated output of <see cref="DomainIdentifierAttribute"/> is correct.</remarks>
    public class NoOpCodeProcessor : CodeProcessor
    {
        public NoOpCodeProcessor(CodeDomProvider codeDomProvider) : base(codeDomProvider)
        {
        }

        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
        }
    }

    /// <summary>
    /// This base class exists solely to test whether [DomainIdentifier] can be inherited
    /// </summary>
    [DomainIdentifier("PeopleDomain", CodeProcessor = typeof(NoOpCodeProcessor))]
    public class BaseDomainService : DomainService
    {
    }

    /// <summary>
    /// This class exposes a DomainService over a peoples list
    /// </summary>
    [EnableClientAccess]
    public class PeopleDomainService : BaseDomainService
    {
        private readonly List<Person> _people = [];

        [Query]
        public IQueryable<Person> GetPersons()
        {
            return this._people.AsQueryable<Person>();
        }
    }
}
#endif
