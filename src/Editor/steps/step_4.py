class Worker: 
	def get_paid(self, amount):
		self.money += amount

class Developer:
	def create_system(self, spec):
		self.current_spec = spec
		self.write_code()
		self.install_system()

class WorkingDeveloper(Worker,Developer):
	pass