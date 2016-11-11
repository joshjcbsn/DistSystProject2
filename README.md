# DistSystProject2
Requirements
Safety: 
1. only 1 value that was proposed may be chosen
2. only a single value may be chosen
3. a process never learns a value has been chosen unless it actually has
learn = written in log => may read from the log

Liveness:
No precise requirement
goal: eventually some proposed value is chosen

Invariants
(P1) 
an acceptor must accept the first value it recieves
	- want things to work when no faliures and no competing proposals
Suppose 2 proposals prop A + prop B, K = 2m+1 acceptors 
	prop A gets m accepts 	prop B gets m+1 acceptors
	one faliure = can't learn which proposal was accepted
acceptors must be able to accept more than one proposal

each proposal has a unique proposal number n
	proposal(n,v)
	n = prop. #
	v = val
a value v is chosen whena single proposal (n,v) is accepted by a majority of acceptors
multiple proposals can be chosen but all must have same value
(P2a)
If a proposal (n,v) is chosen, every higher numbered proposal accepted by any acceptor has value v
	(P2a) => (P2)
(P2b) 
If proposal (n,v) is chosen, every higher numbered proposal issued by any proposer has value V
	(P2b) => (P2a) => (P2)

as a proposer, when can I propose my value?
	when no value has been chosen;
		when a majority have not accepted a single value
how to check if a proposal has been chosen?

(P2c)
For any v and n, if proposal (n,v) is issued then there is a majority acceptor such that either 
a. no acceptor in S has accepted any value
b. v is the value of the highest number proposal with number less than n that was accepted by any acceptor in S
