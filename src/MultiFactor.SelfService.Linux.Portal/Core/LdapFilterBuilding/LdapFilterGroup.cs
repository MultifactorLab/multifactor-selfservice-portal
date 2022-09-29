﻿using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions;

namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding
{
    public class LdapFilterGroup : ILdapFilterGroup
    {
        public LdapFilterOperator Operator { get; }

        private readonly List<ILdapFilter> _filters = new ();
        public IReadOnlyList<ILdapFilter> Filters => _filters.AsReadOnly();

        private LdapFilterGroup(LdapFilterOperator op)
        {
            Operator = op;
        }

        public static ILdapFilterGroup Create(LdapFilterOperator op)
        {
            return new LdapFilterGroup(op);
        }

        public ILdapFilter Add(params ILdapFilter[] filters)
        {
            _filters.AddRange(filters);
            return this;
        }

        public ILdapFilter Add(string attribute, string value) => Add(LdapFilter.Create(attribute, value));     

        public ILdapFilter And(ILdapFilter filter)
        {
            return new LdapFilterGroup(LdapFilterOperator.And).Add(this, filter);
        }

        public ILdapFilter Or(ILdapFilter filter)
        {
            return new LdapFilterGroup(LdapFilterOperator.Or).Add(this, filter);
        }

        public ILdapFilter Not()
        {
            foreach (var filter in _filters)
            {
                filter.Not();
            }

            return this;
        }

        public string Build() => $"({GetOperatorRepresentation(Operator)}{string.Join("", _filters.Select(x => x.Build()))})";

        public override string ToString() => Build();     

        private static string GetOperatorRepresentation(LdapFilterOperator op)
        {
            switch (op)
            {
                case LdapFilterOperator.And: return "&";
                case LdapFilterOperator.Or: return "|";
                default: throw new NotImplementedException("Unexpected operator");
            }
        }

        public ILdapFilter And(string attribute, string value)
        {
            throw new NotImplementedException();
        }

        public enum LdapFilterOperator
        {
            /// <summary>
            /// &
            /// </summary>
            And,

            /// <summary>
            /// |
            /// </summary>
            Or
        }
    }
}
