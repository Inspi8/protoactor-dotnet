﻿// -----------------------------------------------------------------------
//   <copyright file="MemberStrategy.cs" company="Asynkron AB">
//       Copyright (C) 2015-2020 Asynkron AB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Proto.Cluster.Partition;

namespace Proto.Cluster
{
    public interface IMemberStrategy
    {
        List<Member> GetAllMembers();
        void AddMember(Member member);

        void RemoveMember(Member member);
        string GetActivatorAddress();
        
        Member GetActivator();
    }

    internal class SimpleMemberStrategy : IMemberStrategy
    {
        private readonly List<Member> _members;
        private readonly Rendezvous _rdv;
        private readonly RoundRobinMemberSelector _rr;

        public SimpleMemberStrategy()
        {
            _members = new List<Member>();
            _rdv = new Rendezvous();
            _rr = new RoundRobinMemberSelector(this);
        }

        public List<Member> GetAllMembers() => _members;

        //TODO: account for Member.MemberId
        public void AddMember(Member member)
        {
            // Avoid adding the same member twice
            if (_members.Any(x => x.Address == member.Address))
            {
                return;
            }

            _members.Add(member);
            _rdv.UpdateMembers(_members);
        }

        //TODO: account for Member.MemberId
        public void RemoveMember(Member member)
        {
            _members.RemoveAll(x => x.Address == member.Address);
            _rdv.UpdateMembers(_members);
        }

        public string GetActivatorAddress() => _rr.GetMemberAddress();
        public Member GetActivator() => _rr.GetMember();
    }
}