class Person:
	def __init__(self, name):
		self.name = name

	def talk_to(self, other_person):
		""" This is documentation for this method """
		print "Hello %s" % other_person.name