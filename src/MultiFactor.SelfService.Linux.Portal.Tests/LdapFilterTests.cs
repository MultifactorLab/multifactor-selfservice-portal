using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class LdapFilterTests
    {
        [Fact]
        public void Build_simple_filter()
        {
            var filter = LdapFilter.Create("objectclass", "user");
            var s = filter.Build();

            Assert.Equal("(objectclass=user)", s);
        }

        [Fact]
        public void Build_simple_filter_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not();
            var s = filter.Build();

            Assert.Equal("(!(objectclass=user))", s);
        }

        [Fact]
        public void Build_simple_filter_or()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person"));
            var s = filter.Build();

            Assert.Equal("(|(objectclass=user)(objectclass=person))", s);
        }

        [Fact]
        public void Build_simple_filter_or_short_version()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or("objectclass", "person");
            var s = filter.Build();

            Assert.Equal("(|(objectclass=user)(objectclass=person))", s);
        }

        [Fact]
        public void Build_simple_filter_or_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person")).Not();
            var s = filter.Build();

            Assert.Equal("(|(!(objectclass=user))(!(objectclass=person)))", s);
        }

        [Fact]
        public void Build_simple_filter_or_first_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not().Or(LdapFilter.Create("objectclass", "person"));
            var s = filter.Build();

            Assert.Equal("(|(!(objectclass=user))(objectclass=person))", s);
        }

        [Fact]
        public void Build_simple_filter_or_second_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Or(LdapFilter.Create("objectclass", "person").Not());
            var s = filter.Build();

            Assert.Equal("(|(objectclass=user)(!(objectclass=person)))", s);
        }

        [Fact]
        public void Build_simple_filter_and()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "user.name"));
            var s = filter.Build();

            Assert.Equal("(&(objectclass=user)(uid=user.name))", s);
        }

        [Fact]
        public void Build_simple_filter_and_short_version()
        {
            var filter = LdapFilter.Create("objectclass", "user").And("uid", "user.name");
            var s = filter.Build();

            Assert.Equal("(&(objectclass=user)(uid=user.name))", s);
        }

        [Fact]
        public void Build_simple_filter_and_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "user.name")).Not();
            var s = filter.Build();

            Assert.Equal("(&(!(objectclass=user))(!(uid=user.name)))", s);
        }

        [Fact]
        public void Build_simple_filter_and_first_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").Not().And(LdapFilter.Create("uid", "user.name"));
            var s = filter.Build();

            Assert.Equal("(&(!(objectclass=user))(uid=user.name))", s);
        }

        [Fact]
        public void Build_simple_filter_and_second_inner_not()
        {
            var filter = LdapFilter.Create("objectclass", "user").And(LdapFilter.Create("uid", "user.name").Not());
            var s = filter.Build();

            Assert.Equal("(&(objectclass=user)(!(uid=user.name)))", s);
        }

        [Fact]
        public void Build_complex_filter_or_and()
        {
            var filter = LdapFilter.Create("objectclass", "user")
                .Or(LdapFilter.Create("objectclass", "person"))
                .And(LdapFilter.Create("uid", "user.name"));
            var s = filter.Build();

            Assert.Equal("(&(|(objectclass=user)(objectclass=person))(uid=user.name))", s);
        }

        [Fact]
        public void Build_more_complex_filter()
        {
            var filter = LdapFilter.Create("objectclass", "user")
                .Or(LdapFilter.Create("objectclass", "person"))
                .And(LdapFilter.Create("uid", "user.name").Or(LdapFilter.Create("sAMAccountName", "user.name")));

            var s = filter.Build();

            Assert.Equal("(&(|(objectclass=user)(objectclass=person))(|(uid=user.name)(sAMAccountName=user.name)))", s);
        }

        [Fact]
        public void Build_OR_with_params_version_of_constructor()
        {
            var filter = LdapFilter.Create("objectclass", "user", "person");

            var s = filter.Build();

            Assert.Equal("(|(objectclass=user)(objectclass=person))", s);
        }

        [Fact]
        public void Build_OR_with_params_version_of_constructor_if_args_passed_as_arr()
        {
            var filter = LdapFilter.Create("objectclass", new[] { "user", "person" });

            var s = filter.Build();

            Assert.Equal("(|(objectclass=user)(objectclass=person))", s);
        }

        [Fact]
        public void Build_should_throw_ex_if_no_values()
        {
            Assert.Throws<ArgumentException>(() => LdapFilter.Create("objectclass"));
        }

        [Fact]
        public void Build_should_throw_ex_if_no_values_arr()
        {
            Assert.Throws<ArgumentException>(() => LdapFilter.Create("objectclass", Array.Empty<string>()));
        }
    }
}
