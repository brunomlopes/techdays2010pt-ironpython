class Person:
	def __init__(self, name):
		self.name = name

	def talk_to(self, other_person):
		print "Hello %s" % other_person.name