class KnowledgeWorker:
	def __init__(self):
		self.subjects = []
		
class Developer(KnowledgeWorker):
	def implement(self, feature, needed_subject):
		if needed_subject not in self.subjects: raise Exception("Eu não percebo de %s!" % needed_subject)
		self.write_tests_and_code(feature)
		
	def write_tests_and_code(self, feature):
		print "Escrevendo os testes para %s" % feature
		print "Implementando %s" % feature
		
class Learner:
	def learn(self, subject): self.subjects.append(subject)		
		
class TechDaysAttendee(Developer, Learner): pass

Hugo = Developer()
Maria = TechDaysAttendee()
Maria.learn("ironpython")