from System.IO import Path
import clr
clr.AddReferenceToFileAndPath(Path.GetFullPath(r"lib\HtmlAgilityPack.dll"))

from HtmlAgilityPack import HtmlWeb, HtmlEntity
html = HtmlWeb().Load("http://www.techdays2010.com/Event/Speaker/Index", "GET")

speakerNodes = html.DocumentNode.SelectNodes("//div[@class='Speaker']")
class Speaker:
	def __init__(self, node):
		self.name = HtmlEntity.DeEntitize(node.SelectNodes("./h2")[0].InnerText.strip())
		titleNode = node.SelectNodes("./p[@class='title']")
		if titleNode != None:
			self.title = titleNode[0].InnerText.strip()
		else:
			self.title = None
		self.link = node.SelectNodes("./h2/a")[0].Attributes["href"].Value
		
	def __str__(self): 
		return "Speaker '%s'" % self.name

speakers = [Speaker(node) for node in speakerNodes]
print ",".join(s.name for s in speakers[1:12])
