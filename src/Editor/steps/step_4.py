class KnowledgeWorker:
	def __init__(self):
		self.subjects = []
		
class Developer(KnowledgeWorker):
	def implement(self, feature, needed_subject):
		if needed_subject not in self.subjects: raise Exception("I don't know about %s" % needed_subject)
		self.write_tests_and_code(feature)
		
	def write_tests_and_code(self, feature):
		print "Writing tests for %s" % feature
		print "Writing code for %s" % feature
		
class Learner:
	def learn(self, subject): self.subjects.append(subject)		
		
class TechDaysAttendee(Developer, Learner): pass

Abel = Developer()
Cain = TechDaysAttendee()
Cain.learn("ironpython")