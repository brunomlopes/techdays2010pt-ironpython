class Person:
	def __init__(self, name):
		self.name = name

	def talk_to(self, other_person):
		""" This is documentation for this method """
		print "Bom dia %s, chamo-me %s" % (other_person.name, self.name)

Person("Hugo").talk_to(Person("Maria"))
