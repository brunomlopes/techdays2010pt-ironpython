class Developer:
	def __init__(self):
		self.subjects = []

	def implement(self, feature, needed_subject):
		if needed_subject not in self.subjects:
			raise Exception("I don't know about %s" % needed_subject)
		self.write_tests(feature)
		self.write_code(feature)
		
	def write_tests(self, feature): print "Writing tests for %s" % feature
	def write_code(self, feature): print "Writing code for %s" % feature
	

class TechDaysAttendee(Developer): 
	def learn(self, subject):
		self.subjects.append(subject)


Abel = Developer()

