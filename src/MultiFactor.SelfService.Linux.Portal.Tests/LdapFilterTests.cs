using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class LdapFilterTests
    {
        [Fact]
        public void Build_simple_filter()
        {
            var filter = LdapFilter.Create("objectclass", "user");
            var s = filter.ToString();

            Assert.Equal("(objectclass=user)", s);
        }

        [Fact]
        public void Build_simple_filter_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not();
            var s = filter.ToString();

            Assert.Equal("(!(objectclass=user))", s);
        }

        [Fact]
        public void Build_simple_filter_or()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person"));
            var s = filter.ToString();

            Assert.Equal("(|(objectclass=user)(objectclass=person))", s);
        }

        [Fact]
        public void Build_simple_filter_or_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person")).Not();
            var s = filter.ToString();

            Assert.Equal("(|(!(objectclass=user))(!(objectclass=person)))", s);
        }

        [Fact]
        public void Build_simple_filter_or_first_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not().Or(LdapFilter.Create("objectclass", "person"));
            var s = filter.ToString();

            Assert.Equal("(|(!(objectclass=user))(objectclass=person))", s);
        }

        [Fact]
        public void Build_simple_filter_or_second_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person").Not());
            var s = filter.ToString();

            Assert.Equal("(|(objectclass=user)(!(objectclass=person)))", s);
        }

        [Fact]
        public void Build_simple_filter_and()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "a.pashkov"));
            var s = filter.ToString();

            Assert.Equal("(&(objectclass=user)(uid=a.pashkov))", s);
        }

        [Fact]
        public void Build_simple_filter_and_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "a.pashkov")).Not();
            var s = filter.ToString();

            Assert.Equal("(&(!(objectclass=user))(!(uid=a.pashkov)))", s);
        }

        [Fact]
        public void Build_simple_filter_and_first_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not().And(LdapFilter.Create("uid", "a.pashkov"));
            var s = filter.ToString();

            Assert.Equal("(&(!(objectclass=user))(uid=a.pashkov))", s);
        }

        [Fact]
        public void Build_simple_filter_and_second_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "a.pashkov").Not());
            var s = filter.ToString();

            Assert.Equal("(&(objectclass=user)(!(uid=a.pashkov)))", s);
        }

        [Fact]
        public void Build_complex_filter_or_and()
        {
            var filter = LdapFilter.Create("objectclass", "user")
                .Or(LdapFilter.Create("objectclass", "person"))
                .And(LdapFilter.Create("uid", "a.pashkov"));
            var s = filter.ToString();

            Assert.Equal("(&(|(objectclass=user)(objectclass=person))(uid=a.pashkov))", s);
        }

        [Fact]
        public void Build_more_complex_filter()
        {
            var filter = LdapFilter.Create("objectclass", "user")
                .Or(LdapFilter.Create("objectclass", "person"))
                .And(LdapFilter.Create("uid", "a.pashkov").Or(LdapFilter.Create("sAMAccountName", "a.pashkov")));

            var s = filter.ToString();

            Assert.Equal("(&(|(objectclass=user)(objectclass=person))(|(uid=a.pashkov)(sAMAccountName=a.pashkov)))", s);
        }
    }

}