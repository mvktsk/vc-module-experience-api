using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schema;
using VirtoCommerce.ExperienceApiModule.XProfile.Commands;
using VirtoCommerce.ExperienceApiModule.XProfile.Requests;
using VirtoCommerce.ExperienceApiModule.XProfile.Services;

namespace VirtoCommerce.ExperienceApiModule.XProfile.Schemas
{
    public class ProfileSchema : ISchemaBuilder
    {
        public const string _commandName = "command";

        private readonly IMediator _mediator;
        private readonly IDataLoaderContextAccessor _dataLoader;
        private readonly IMemberServiceX _memberService;

        public ProfileSchema(IMediator mediator, IDataLoaderContextAccessor dataLoader, IMemberServiceX memberService)
        {
            _mediator = mediator;
            _dataLoader = dataLoader;
            _memberService = memberService;
        }

        public void Build(ISchema schema)
        {
            _ = schema.Query.AddField(new FieldType
            {
                Name = "customer",
                Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "user id" }),
                Type = GraphTypeExtenstionHelper.GetActualType<ProfileType>(),
                Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var loader = _dataLoader.Context.GetOrAddBatchLoader<string, Profile>("profileLoader", (id) => LoadProfileAsync(_mediator, id, context.SubFields.Values.GetAllNodesPaths()));
                    return await loader.LoadAsync(context.GetArgument<string>("id"));
                })
            });

            /// <example>
            /// This is a sample request for organization users (connection) query. Valid organizationId required
            ///{
            ///    organizationUsers(command: { organizationId: "2e4c562f-f51c-4d49-84c3-8ba9f661aee7", userId: "62223176-92db-4bf7-963a-15a07928095c"}){
            ///        totalCount items { contact{ firstName } userName }
            ///    }
            ///}
            /// </example>
            var connectionBuilder = GraphTypeExtenstionHelper.CreateConnection<ProfileType, object>()
              .Name("organizationUsers")
              .Argument<NonNullGraphType<GetOrganizationUsersInputType>>(_commandName, "Query command")
              .Unidirectional()
              .PageSize(20);
            connectionBuilder.ResolveAsync(ResolveOrganizationUsersConnectionAsync);
            _ = schema.Query.AddField(connectionBuilder.FieldType);

            _ = schema.Mutation.AddField(FieldBuilder.Create<UserUpdateInfo, Profile>(GraphTypeExtenstionHelper.GetActualType<ProfileType>())
                            .Name("updateAccount")
                            .Argument<NonNullGraphType<UserUpdateInfoInputType>>("userUpdateInfo")
                            .ResolveAsync(async context =>
                            {
                                return await _memberService.UpdateContactAsync(context.GetArgument<UserUpdateInfo>("userUpdateInfo"));
                            }).FieldType);

            /// <example>
            /// This is a sample mutation to updatePhoneNumber.
            /// mutation($input: PhoneNumberUpdateInfoInputType!){
            ///     updatePhoneNumber(input: $input){ succeeded }
            /// }
            /// query variables:
            /// {
            ///   "input": { "id": "be77bbe9-91a7-42bf-b253-9ed3a976af08", "phoneNumber": "666-5556" }
            /// }
            ///}
            /// </example>
            _ = schema.Mutation.AddField(FieldBuilder.Create<PhoneNumberUpdateInfo, IdentityResult>(typeof(IdentityResultType))
                            .Name("updatePhoneNumber")
                            .Argument<NonNullGraphType<PhoneNumberUpdateInfoInputType>>("input")
                            .ResolveAsync(async context =>
                            {
                                return await _memberService.UpdatePhoneNumberAsync(context.GetArgument<PhoneNumberUpdateInfo>("input"));
                            }).FieldType);


            /// <example>
            /// This is a sample mutation to removePhoneNumber.
            /// mutation ($input: String!){
            ///   removePhoneNumber(input: $input){ succeeded }
            /// }
            /// query variables:
            /// {
            ///   "input": "be77bbe9-91a7-42bf-b253-9ed3a976af08"
            /// }
            /// </example>
            _ = schema.Mutation.AddField(FieldBuilder.Create<string, IdentityResult>(typeof(IdentityResultType))
                            .Name("removePhoneNumber")
                            .Argument<NonNullGraphType<StringGraphType>>("input")
                            .ResolveAsync(async context =>
                            {
                                return await _memberService.RemovePhoneNumberAsync(context.GetArgument<string>("input"));
                            }).FieldType);

            //schema.Mutation.AddField(FieldBuilder.Create<IList<string>, string>(typeof(StringGraphType))
            //                .Name("testSimple")
            //                .Argument<NonNullGraphType<StringGraphType>>("customerId")
            //                .Resolve(context =>
            //                {
            //                    return context.GetArgument<string>("customerId");
            //                }).FieldType);

            //schema.Mutation.AddField(FieldBuilder.Create<IList<string>, string>(typeof(StringGraphType))
            //                .Name("testList")
            //                .Argument<ListGraphType<StringGraphType>>("items")
            //                .Resolve(context =>
            //                {
            //                    var items = context.GetArgument<IList<string>>("items");
            //                    return items.FirstOrDefault();
            //                }).FieldType);

            _ = schema.Mutation.AddField(FieldBuilder.Create<IList<Address>, Contact>(GraphTypeExtenstionHelper.GetActualType<ContactType>())
                            .Name("updateAddresses")
                            .Argument<NonNullGraphType<StringGraphType>>("customerId")
                            .Argument<NonNullGraphType<ListGraphType<AddressInputType>>>("addresses")
                            .ResolveAsync(async context =>
                            {
                                return await _memberService.UpdateContactAddressesAsync(
                                             context.GetArgument<string>("customerId"),
                                             context.GetArgument<IList<Address>>("addresses"));
                            }).FieldType);

            _ = schema.Mutation.AddField(FieldBuilder.Create<OrganizationUpdateInfo, Organization>(GraphTypeExtenstionHelper.GetActualType<OrganizationInputType>())
                            .Name("updateOrganization")
                            .Argument<NonNullGraphType<OrganizationUpdateInfoInputType>>("input")
                            .ResolveAsync(async context =>
                            {
                                return await _memberService.UpdateOrganizationAsync(
                                             context.GetArgument<OrganizationUpdateInfo>("input"));
                            }).FieldType);

            /// <example>
            /// This is a sample mutation to createOrganization.
            /// mutation($command: CreateOrganizationType!){
            ///     createOrganization(command: $command){
            ///         succeeded errors { description }
            ///     }
            /// }
            /// query variables:
            /// {
            ///     "command": {
            
            ///     }
            /// }
            /// </example>
            _ = schema.Mutation.AddField(FieldBuilder.Create<OrganizationRequest, Organization>(typeof(OrganizationInputType))
                            .Name("createOrganization")
                            .Argument<NonNullGraphType<OrganizationInputType>>(_commandName)
                            .ResolveAsync(async context => await _mediator.Send(context.GetArgument<CreateOrganizationCommand>(_commandName)))
                            .FieldType);

            /// <example>
            /// This is a sample mutation to lockUser.
            /// mutation lockUser($input: LockUserInputType!){
            ///     lockUser(command: $input){
            ///         succeeded
            ///     }
            /// }
            /// query variables:
            /// {
            ///     "input": { "userId": "a9fa82c5-d1b9-4838-b3ba-bec4e7b358dc" }
            /// }
            /// </example>
            _ = schema.Mutation.AddField(FieldBuilder.Create<Profile, IdentityResult>(typeof(IdentityResultType))
                            .Name("lockUser")
                            .Argument<NonNullGraphType<LockUserInputType>>(_commandName)
                            .ResolveAsync(async context => await _mediator.Send(context.GetArgument<LockUserCommand>(_commandName)))
                            .FieldType);

            /// Check the lockUser above for a sample.
            _ = schema.Mutation.AddField(FieldBuilder.Create<Profile, IdentityResult>(typeof(IdentityResultType))
                            .Name("unlockUser")
                            .Argument<NonNullGraphType<UnlockUserInputType>>(_commandName)
                            .ResolveAsync(async context => await _mediator.Send(context.GetArgument<UnlockUserCommand>(_commandName)))
                            .FieldType);
        }

        public static async Task<IDictionary<string, Profile>> LoadProfileAsync(IMediator mediator, IEnumerable<string> ids, IEnumerable<string> includeFields)
        {
            var response = await mediator.Send(new LoadProfileRequest { Id = ids.FirstOrDefault(), IncludeFields = includeFields });
            return response.Results;
        }

        private async Task<object> ResolveOrganizationUsersConnectionAsync(IResolveConnectionContext<object> context)
        {
            var first = context.First;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());
            var command = context.GetArgument<GetOrganizationUsersCommand>(_commandName);
            command.Take = first ?? 20;
            command.Skip = skip;
            var response = await _mediator.Send(command);

            var result = new Connection<Profile>()
            {
                Edges = response.Results.Select((x, index) =>
                        new Edge<Profile>()
                        {
                            Cursor = (skip + index).ToString(),
                            Node = x,
                        })
                    .ToList(),
                PageInfo = new PageInfo()
                {
                    HasNextPage = response.TotalCount > (skip + first),
                    HasPreviousPage = skip > 0,
                    StartCursor = skip.ToString(),
                    EndCursor = Math.Min(response.TotalCount, (int)(skip + first)).ToString()
                },
                TotalCount = response.TotalCount,
            };

            return result;
        }
    }
}